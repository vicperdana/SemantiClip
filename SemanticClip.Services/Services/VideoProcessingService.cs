using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;
using SemanticClip.Services.Steps;
using SemanticClip.Services.Utilities;

namespace SemanticClip.Services;



public class VideoProcessingService : IVideoProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly Kernel _kernel; // Single kernel for both chat and audio-to-text
    private readonly ILogger<VideoProcessingService> _logger;
    private Action<VideoProcessingProgress>? _progressCallback;

    public VideoProcessingService(IConfiguration configuration, ILogger<VideoProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        try
        {
            // Create the kernel
            var builder = Kernel.CreateBuilder();
            
            // Try to configure Azure OpenAI for chat completion if available
            var azureOpenAIEndpoint = _configuration["AzureOpenAI:Endpoint"];
            var azureOpenAIKey = _configuration["AzureOpenAI:ApiKey"];
            var contentDeployment = _configuration["AzureOpenAI:ContentDeploymentName"];
            
            if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey) && !string.IsNullOrEmpty(contentDeployment))
            {
                builder.AddAzureOpenAIChatCompletion(
                    contentDeployment,
                    azureOpenAIEndpoint,
                    azureOpenAIKey);
                _logger.LogInformation("Azure OpenAI chat completion configured");
            }
            else
            {
                _logger.LogWarning("No chat completion service configured - video processing will be limited");
            }

            // Try to configure Azure OpenAI Whisper if available
            var whisperDeployment = _configuration["AzureOpenAI:WhisperDeploymentName"];
            if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey) && !string.IsNullOrEmpty(whisperDeployment))
            {
                builder.AddAzureOpenAIAudioToText(
                    whisperDeployment,
                    azureOpenAIEndpoint,
                    azureOpenAIKey);
                _logger.LogInformation("Azure OpenAI audio-to-text configured");
            }
            else
            {
                _logger.LogWarning("No audio-to-text service configured - audio transcription will not be available");
            }

            _kernel = builder.Build();

            // Configure Azure AI Agent if available
            var connectionString = _configuration["AzureAIAgent:ConnectionString"];
            var chatModelId = _configuration["AzureAIAgent:ChatModelId"];
            var vectorStoreId = _configuration["AzureAIAgent:VectorStoreId"];
            var maxEvaluationsStr = _configuration["AzureAIAgent:MaxEvaluations"];

            if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(chatModelId) && !string.IsNullOrEmpty(vectorStoreId) && !string.IsNullOrEmpty(maxEvaluationsStr))
            {
                AzureAIAgentConfig.ConnectionString = connectionString;
                AzureAIAgentConfig.ChatModelId = chatModelId;
                AzureAIAgentConfig.VectorStoreId = vectorStoreId;
                AzureAIAgentConfig.MaxEvaluations = int.Parse(maxEvaluationsStr);
                _logger.LogInformation("Azure AI Agent configured");
            }
            else
            {
                _logger.LogWarning("Azure AI Agent configuration incomplete - agent features will not be available");
            }

            // Configure MCP if available
            var githubToken = _configuration["GitHub:PersonalAccessToken"];
            if (!string.IsNullOrEmpty(githubToken))
            {
                MCPConfig.GitHubPersonalAccessToken = githubToken;
                _logger.LogInformation("GitHub MCP configured");
            }
            else
            {
                _logger.LogWarning("GitHub Personal Access Token not configured - GitHub features will not be available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing VideoProcessingService - service will operate with limited functionality");
            // Create a minimal kernel as fallback
            _kernel = Kernel.CreateBuilder().Build();
        }
    }

    public void SetProgressCallback(Action<VideoProcessingProgress>? callback)
    {
        _progressCallback = callback;
    }

    public void UpdateProgress(string status, int percentage, string currentOperation = "", string? error = null)
    {
        if (_progressCallback != null)
        {
            var progress = new VideoProcessingProgress
            {
                Status = status,
                Percentage = percentage,
                CurrentOperation = currentOperation,
                Error = error
            };
            _progressCallback(progress);
        }
    }

    public async Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request)
    {
        return await ProcessVideoAsync(request, null);
    }
    
    public async Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request, Action<VideoProcessingProgress>? progressCallback = null)
    {
        try
        {
            _logger.LogInformation("Starting video processing for file: {FileName}", request.FileName);
            
            // Set the progress callback for this specific operation
            var originalCallback = _progressCallback;
            _progressCallback = progressCallback;
            
            UpdateProgress("Starting", 5, "Initializing video processing workflow...");
            
            // Create a new Semantic Kernel process
            _logger.LogInformation("Creating Semantic Kernel process builder");
            ProcessBuilder processBuilder = new("VideoProcessingWorkflow");
            
            // Add the processing steps
            _logger.LogInformation("Adding processing steps to workflow");
            var prepareVideoStep = processBuilder.AddStepFromType<PrepareVideoStep>();
            var transcribeVideoStep = processBuilder.AddStepFromType<TranscribeVideoStep>();
            var generateBlogPostStep = processBuilder.AddStepFromType<GenerateBlogPostStep>();
            
            // Note: Skip EvaluateBlogPostStep since Azure AI Agent is not configured
            
            UpdateProgress("Processing", 15, "Setting up workflow steps...");
            
            // Orchestrate the process
            _logger.LogInformation("Setting up workflow orchestration");
            processBuilder
                .OnInputEvent("Start")
                .SendEventTo(new(prepareVideoStep, functionName: PrepareVideoStep.Functions.PrepareVideo,
                    parameterName: "request"));

            prepareVideoStep
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(transcribeVideoStep,
                    functionName: TranscribeVideoStep.Functions.TranscribeVideo,
                    parameterName: "videoPath"));

            transcribeVideoStep
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(generateBlogPostStep,
                    functionName: GenerateBlogPostStep.Functions.GenerateBlogPost,
                    parameterName: "transcript"));

            // GenerateBlogPostStep is now the final step - no need to route to EvaluateBlogPostStep
            
            UpdateProgress("Processing", 25, "Starting video processing...");
            
            // Build the process
            _logger.LogInformation("Building the process");
            var process = processBuilder.Build();
            
            // Execute the workflow
            _logger.LogInformation("Starting process execution with event: Start");
            var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "Start", Data = request});
            
            _logger.LogInformation("Getting final state from process");
            var finalState = await initialResult.GetStateAsync();
            
            UpdateProgress("Processing", 90, "Finalizing results...");
            
            _logger.LogInformation("Converting final state to metadata");
            var finalCompletion = finalState.ToProcessStateMetadata();
            
            _logger.LogInformation("Available steps in final state: {Steps}", 
                finalCompletion.StepsState != null ? string.Join(", ", finalCompletion.StepsState.Keys) : "None");
            
            // Check if GenerateBlogPostStep exists and has the expected state
            if (finalCompletion.StepsState?.ContainsKey("GenerateBlogPostStep") != true)
            {
                _logger.LogError("GenerateBlogPostStep not found in final state");
                throw new InvalidOperationException("GenerateBlogPostStep not found in final state");
            }
            
            var generateStepState = finalCompletion.StepsState["GenerateBlogPostStep"].State;
            _logger.LogInformation("GenerateBlogPostStep state type: {StateType}", generateStepState?.GetType().Name ?? "null");
            
            if (generateStepState is not BlogPostProcessingResponse blogPostProcessingResponse)
            {
                _logger.LogError("Failed to cast GenerateBlogPostStep state to BlogPostProcessingResponse. Actual type: {ActualType}", 
                    generateStepState?.GetType().Name ?? "null");
                throw new InvalidOperationException($"Failed to retrieve completion step state. Expected BlogPostProcessingResponse, got {generateStepState?.GetType().Name ?? "null"}");
            }
            
            // Convert BlogPostProcessingResponse to VideoProcessingResponse
            var videoProcessingResponse = blogPostProcessingResponse.VideoProcessingResponse;
            
            _logger.LogInformation("Video processing completed successfully. Response type: {ResponseType}", videoProcessingResponse.GetType().Name);
            UpdateProgress("Completed", 100, "Video processing completed successfully");
            
            // Restore original callback
            _progressCallback = originalCallback;
            
            return videoProcessingResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video: {ErrorMessage}", ex.Message);
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }
}