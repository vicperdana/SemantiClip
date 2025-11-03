using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Agents.AI.Workflows;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;
using SemanticClip.Services.Executors;
using SemanticClip.Services.Utilities;

namespace SemanticClip.Services;

public class BlogPublishingService : IBlogPublishingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlogPublishingService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BlogPublishingService(
        IConfiguration configuration, 
        ILogger<BlogPublishingService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Set up MCP configuration
        var githubToken = _configuration["GitHub:PersonalAccessToken"];
        if (!string.IsNullOrEmpty(githubToken))
        {
            MCPConfig.GitHubPersonalAccessToken = githubToken;
        }
    }

    public async Task<BlogPublishingResponse> PublishBlogPostAsync(BlogPostPublishRequest request)
    {
        try
        {
            _logger.LogInformation("Starting blog post publishing workflow");
            
            // Create executor
            var publishExecutor = _serviceProvider.GetRequiredService<PublishBlogPostExecutor>();
            
            // Build simple workflow with single executor
            WorkflowBuilder builder = new(publishExecutor);
            builder.WithOutputFrom(publishExecutor);
            var workflow = builder.Build();
            
            // Execute
            _logger.LogInformation("Executing blog publishing workflow");
            Run run = await InProcessExecution.RunAsync(workflow, request);
            
            // Get result
            BlogPublishingResponse? result = null;
            foreach (WorkflowEvent evt in run.NewEvents)
            {
                if (evt is ExecutorCompletedEvent completedEvent)
                {
                    _logger.LogInformation("Executor completed: {ExecutorId}", completedEvent.ExecutorId);
                    
                    if (completedEvent.Data is BlogPublishingResponse response)
                    {
                        result = response;
                    }
                }
            }
            
            return result ?? new BlogPublishingResponse 
            { 
                Success = false, 
                Message = "Workflow did not produce result" 
            };
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
