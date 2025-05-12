using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SemanticClip.Core.Models;
using SemanticClip.Services.Steps;
using SemanticClip.Services.Utilities;

namespace SemanticClip.Services;

public class BlogPublishingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlogPublishingService> _logger;

    public BlogPublishingService(IConfiguration configuration, ILogger<BlogPublishingService> logger)
    {
        _configuration = configuration;
        _logger = logger;

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

            // Start the process
            var process = processBuilder.Build();
            await process.StartAsync(new BlogPostPublishRequest
            {
                BlogPost = request.BlogPost,
                CommitMessage = request.CommitMessage ?? "Published blog post via SemantiClip"
            });

            // Wait for completion
            var result = await process.WaitForCompletionAsync();
            
            return new BlogPublishingResponse
            {
                Success = result.IsSuccess,
                Message = result.IsSuccess ? "Blog post published successfully" : result.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing blog post: {Error}", ex.Message);
            throw;
        }
    }
}
