using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SemanticClip.Core.Models;
using SemanticClip.Services.Steps;
using SemanticClip.Services.Utilities;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;

namespace SemanticClip.Services;

public class BlogPublishingService
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
                throw new InvalidOperationException("Failed to retrieve completion step state");
            }
            
            return blogPublishingResponse;
            
            return blogPublishingResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing blog post: {Error}", ex.Message);
            throw;
        }
    }
}
