using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;
using SemanticClip.Services.Steps;
using SemanticClip.Services.Utilities;

namespace SemanticClip.Services;

public class BlogPublishingService : IBlogPublishingService
{
    private readonly IConfiguration _configuration;
    private readonly Kernel _kernel;
    private readonly ILogger<BlogPublishingService> _logger;

    public BlogPublishingService(IConfiguration configuration, ILogger<BlogPublishingService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Create the kernel
        var builder = Kernel.CreateBuilder();
        
        // Use Azure OpenAI for chat completion agent
        builder.AddAzureOpenAIChatCompletion(
            _configuration["AzureOpenAI:ContentDeploymentName"]!,
            _configuration["AzureOpenAI:Endpoint"]!,
            _configuration["AzureOpenAI:ApiKey"]!);
        
        _kernel = builder.Build();

        // Set up MCP configuration
        MCPConfig.GitHubPersonalAccessToken = _configuration["GitHub:PersonalAccessToken"]!;
    }

    public async Task<BlogPublishingResponse> PublishBlogPostAsync(BlogPostPublishRequest request)
    {
        try
        {
            // Create a new Semantic Kernel process
            ProcessBuilder processBuilder = new("BlogPublishingWorkflow");
            
            // Add the publishing step
            var publishBlogPostStep = processBuilder.AddStepFromType<PublishBlogPostStep>();
            
            // Orchestrate the process
            processBuilder
                .OnInputEvent("Start")
                .SendEventTo(new(publishBlogPostStep, functionName: PublishBlogPostStep.Functions.PublishBlogPost,
                    parameterName: "request"));

            // Build the process
            var process = processBuilder.Build();
            
            // Execute the workflow
            var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "Start", Data = request});
            var finalState = await initialResult.GetStateAsync();
            var finalCompletion = finalState.ToProcessStateMetadata();
            
            // Get the completion step state
            if (finalCompletion.StepsState!["PublishBlogPostStep"].State is not BlogPublishingResponse blogPublishingResponse)
            {
                // Try to get the state from the step state directly
                if (finalCompletion.StepsState.TryGetValue("PublishBlogPostStep", out var stepState) && 
                    stepState.State is BlogPublishingResponse responseFromState)
                {
                    return responseFromState;
                }
                
                // Fallback to checking the final state
                if (finalCompletion.State is IDictionary<string, object> stateData && 
                    stateData.TryGetValue("response", out var responseObj) && 
                    responseObj is BlogPublishingResponse response)
                {
                    return response;
                }
                
                throw new InvalidOperationException("Failed to retrieve completion step state or event data");
            }
            
            return blogPublishingResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing blog post: {Error}", ex.Message);
            return new BlogPublishingResponse 
            { 
                Success = false, 
                Message = $"Error publishing blog post: {ex.Message}" 
            };
        }
    }
}
