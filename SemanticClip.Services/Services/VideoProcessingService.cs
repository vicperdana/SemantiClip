using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
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

        // Create the kernel
        var builder = Kernel.CreateBuilder();
        
        /* Use Azure OpenAI for chat completion
        builder.AddAzureOpenAIChatCompletion(
            _configuration["AzureOpenAI:ContentDeploymentName"]!,
            _configuration["AzureOpenAI:Endpoint"]!,
            _configuration["AzureOpenAI:ApiKey"]!);*/
        
        // Use local SLM for chat completion
#pragma warning disable SKEXP0070
        builder.AddOllamaChatCompletion(
            modelId: _configuration["LocalSLM:ModelId"]!,
            endpoint: new Uri(_configuration["LocalSLM:Endpoint"]!)
        );
        #pragma warning restore SKEXP0070
        
        #pragma warning disable SKEXP0010
        builder.AddAzureOpenAIAudioToText(
            _configuration["AzureOpenAI:WhisperDeploymentName"]!,
            _configuration["AzureOpenAI:Endpoint"]!,
            _configuration["AzureOpenAI:ApiKey"]!);
        #pragma warning restore SKEXP0010
        _kernel = builder.Build();

        // Create the agents client for Azure AI Agent
        AzureAIAgentConfig.ConnectionString = _configuration["AzureAIAgent:ConnectionString"]!;
        AzureAIAgentConfig.ChatModelId = _configuration["AzureAIAgent:ChatModelId"]!;
        AzureAIAgentConfig.VectorStoreId = _configuration["AzureAIAgent:VectorStoreId"]!;
        AzureAIAgentConfig.MaxEvaluations = int.Parse(_configuration["AzureAIAgent:MaxEvaluations"]!);
        
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
        try
        {
            // Create a new Semantic Kernel process
            #pragma warning disable SKEXP0080
            ProcessBuilder processBuilder = new("VideoProcessingWorkflow");
            
            // Add the processing steps
            var prepareVideoStep = processBuilder.AddStepFromType<PrepareVideoStep>();
            var transcribeVideoStep = processBuilder.AddStepFromType<TranscribeVideoStep>();
            var generateBlogPostStep = processBuilder.AddStepFromType<GenerateBlogPostStep>();
            var evaluateBlogPostStep = processBuilder.AddStepFromType<EvaluateBlogPostStep>();
            //var completionStep = processBuilder.AddStepFromType<CompletionStep>();
            
            
            // Orchestrate the workflow
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

            generateBlogPostStep
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(evaluateBlogPostStep,
                    functionName: EvaluateBlogPostStep.Functions.EvaluateBlogPost,
                    parameterName: "blogstate"));
            
            // Build the process
            var process = processBuilder.Build();
            
            // Execute the workflow
            var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "Start", Data = request});
            var finalState = await initialResult.GetStateAsync();
            var finalCompletion = finalState.ToProcessStateMetadata();
            
            if (finalCompletion.StepsState!["EvaluateBlogPostStep"].State is not VideoProcessingResponse videoProcessingResponse)
            {
                throw new InvalidOperationException("Failed to retrieve completion step state");
            }
            
            return videoProcessingResponse;

#pragma warning restore SKEXP0080
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }

    public async Task<string> TranscribeVideoAsync(string videoPath)
    {
        try{
       #pragma warning disable SKEXP0080
        ProcessBuilder processBuilder = new("VideoProcessingWorkflow");
        var prepareVideoStep = processBuilder.AddStepFromType<PrepareVideoStep>();
        var transcribeVideoStep = processBuilder.AddStepFromType<TranscribeVideoStep>();

        prepareVideoStep
                .OnEvent("VideoPrepared")
                .SendEventTo(new(transcribeVideoStep, parameterName: "videoPath"))
                .SendEventTo(new(transcribeVideoStep, parameterName: "logger"))
                .SendEventTo(new(transcribeVideoStep, parameterName: "progressCallback"))
                .SendEventTo(new(transcribeVideoStep, parameterName: "kernel"));

        // Build the process
        var process = processBuilder.Build();
        
        var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "VideoPrepared", Data = null});
        var finalState = await initialResult.GetStateAsync();
        var finalCompletion = finalState.ToProcessStateMetadata();
        
        if (finalCompletion.State is not string transcript)
        {
            throw new InvalidOperationException("Failed to transcribe video: Invalid state returned from process");
        }
        
        return transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing video");
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }

    
    public async Task<string> GenerateBlogPostAsync(string transcript, List<Chapter> chapters)
    {
        try
        {
            #pragma warning disable SKEXP0080
            ProcessBuilder processBuilder = new("VideoProcessingWorkflow");
            var generateBlogPostStep = processBuilder.AddStepFromType<GenerateBlogPostStep>();

            generateBlogPostStep
                .OnEvent("BlogPostGenerated")
                .SendEventTo(new(generateBlogPostStep, parameterName: "transcript"))
                .SendEventTo(new(generateBlogPostStep, parameterName: "logger"))
                .SendEventTo(new(generateBlogPostStep, parameterName: "progressCallback"))
                .SendEventTo(new(generateBlogPostStep, parameterName: "kernel"));

            // Build the process
            var process = processBuilder.Build();
            
            var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "BlogPostGenerated", Data = null});
            var finalState = await initialResult.GetStateAsync();
            var finalCompletion = finalState.ToProcessStateMetadata();
            
            if (finalCompletion.State is not string blogPost)
            {
                throw new InvalidOperationException("Failed to generate blog post: Invalid state returned from process");
            }
            
            return blogPost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating blog post");
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }
    #pragma warning restore SKEXP0080
    
    
}