using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Agents.AI.Workflows;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;
using SemanticClip.Services.Executors;
using SemanticClip.Services.Utilities;

namespace SemanticClip.Services;



public class VideoProcessingService : IVideoProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Action<VideoProcessingProgress>? _progressCallback;

    public VideoProcessingService(
        IConfiguration configuration, 
        ILogger<VideoProcessingService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
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
            
            // Create executors (injected from DI)
            var prepareExecutor = _serviceProvider.GetRequiredService<PrepareVideoExecutor>();
            var transcribeExecutor = _serviceProvider.GetRequiredService<TranscribeVideoExecutor>();
            var generateExecutor = _serviceProvider.GetRequiredService<GenerateBlogPostExecutor>();
            var evaluateExecutor = _serviceProvider.GetRequiredService<EvaluateBlogPostExecutor>();
            
            UpdateProgress("Processing", 15, "Building workflow...");
            
            // Build the workflow using Agent Framework WorkflowBuilder
            _logger.LogInformation("Building workflow with executors");
            WorkflowBuilder builder = new(prepareExecutor);
            builder
                .AddEdge(prepareExecutor, transcribeExecutor)
                .AddEdge(transcribeExecutor, generateExecutor)
                .AddEdge(generateExecutor, evaluateExecutor)
                .WithOutputFrom(evaluateExecutor); // Final output comes from evaluator
            
            var workflow = builder.Build();
            
            UpdateProgress("Processing", 25, "Executing workflow...");
            
            // Execute the workflow
            _logger.LogInformation("Starting workflow execution");
            Run run = await InProcessExecution.RunAsync(
                workflow, 
                request);
            
            UpdateProgress("Processing", 90, "Finalizing results...");
            
            // Process workflow events
            VideoProcessingResponse? finalResult = null;
            foreach (WorkflowEvent evt in run.NewEvents)
            {
                if (evt is ExecutorCompletedEvent completedEvent)
                {
                    _logger.LogInformation(
                        "Executor completed: {ExecutorId}, Data type: {DataType}", 
                        completedEvent.ExecutorId, 
                        completedEvent.Data?.GetType().Name);
                    
                    // Update progress based on which executor completed
                    switch (completedEvent.ExecutorId)
                    {
                        case "PrepareVideoExecutor":
                            UpdateProgress("Processing", 35, "Video prepared");
                            break;
                        case "TranscribeVideoExecutor":
                            UpdateProgress("Processing", 60, "Transcription completed");
                            break;
                        case "GenerateBlogPostExecutor":
                            UpdateProgress("Processing", 80, "Blog post generated");
                            break;
                        case "EvaluateBlogPostExecutor":
                            UpdateProgress("Processing", 95, "Evaluation completed");
                            // This is our final output
                            if (completedEvent.Data is VideoProcessingResponse response)
                            {
                                finalResult = response;
                            }
                            break;
                    }
                }
            }
            
            if (finalResult == null)
            {
                throw new InvalidOperationException("Workflow did not produce expected output");
            }
            
            UpdateProgress("Completed", 100, "Video processing completed successfully");
            _progressCallback = originalCallback;
            
            return finalResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video: {ErrorMessage}", ex.Message);
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }
}