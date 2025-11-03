using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using SemanticClip.Core.Models;
using SemanticClip.Services.Utilities;

namespace SemanticClip.Services.Executors;

/// <summary>
/// Publishes blog post to GitHub using Model Context Protocol and Azure OpenAI
/// TEMPORARILY DISABLED: Package conflict between Azure.AI.OpenAI 2.1.0 and Microsoft.Extensions.AI.OpenAI
/// </summary>
public sealed class PublishBlogPostExecutor : Executor<BlogPostPublishRequest, BlogPublishingResponse>
{
    private readonly ILogger<PublishBlogPostExecutor> _logger;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly IConfiguration _configuration;
    
    public PublishBlogPostExecutor(
        ILogger<PublishBlogPostExecutor> logger,
        AzureOpenAIClient openAIClient,
        IConfiguration configuration) 
        : base("PublishBlogPostExecutor")
    {
        _logger = logger;
        _openAIClient = openAIClient;
        _configuration = configuration;
    }
    
    public override async ValueTask<BlogPublishingResponse> HandleAsync(
        BlogPostPublishRequest request, 
        IWorkflowContext context)
    {
        _logger.LogWarning("Blog publishing temporarily disabled due to package conflicts");
        
        return await Task.FromResult(new BlogPublishingResponse
        {
            Success = false,
            Message = "Blog publishing temporarily disabled due to package conflicts between Azure.AI.OpenAI 2.1.0 and Microsoft.Extensions.AI.OpenAI"
        });
    }
}
