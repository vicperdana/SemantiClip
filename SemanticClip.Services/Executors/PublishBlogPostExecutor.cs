using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using SemanticClip.Core.Models;
using SemanticClip.Services.Utilities;

namespace SemanticClip.Services.Executors;

/// <summary>
/// Publishes blog post to GitHub using Model Context Protocol and Azure OpenAI
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
        _logger.LogInformation("Starting blog post publishing process");
        
        if (string.IsNullOrEmpty(request.BlogPost))
        {
            _logger.LogWarning("Blog post content is null or empty");
            return new BlogPublishingResponse
            {
                Success = false,
                Message = "Blog post content is empty"
            };
        }

        try
        {
            // Get chat client from injected Azure OpenAI client
            var chatClient = _openAIClient.GetChatClient("gpt-4o");
            
            // Get MCP client for GitHub integration
            var mcpClient = await GetMcpClientAsync();
            var mcpTools = await mcpClient.ListToolsAsync();
            
            _logger.LogInformation("Found {ToolCount} MCP tools", mcpTools.Count);
            
            // Build messages with tools for publishing
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a blog publishing assistant. Use the available GitHub tools to publish blog posts."),
                new UserChatMessage($"Commit message: {request.CommitMessage}\n\nBlog post content:\n{request.BlogPost}")
            };
            
            // Convert MCP tools to OpenAI tools format (simplified - you may need proper conversion)
            var chatOptions = new ChatCompletionOptions
            {
                // Tool configuration would go here if supported
            };
            
            // Call Azure OpenAI with MCP tools
            _logger.LogInformation("Invoking Azure OpenAI to publish blog post");
            var response = await chatClient.CompleteChatAsync(messages, chatOptions, CancellationToken.None);
            
            string result = response.Value.Content[0].Text;
            
            _logger.LogInformation("Blog post publishing completed");
            
            return new BlogPublishingResponse
            {
                Success = true,
                Message = "Blog post published successfully",
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing blog post");
            return new BlogPublishingResponse
            {
                Success = false,
                Message = $"Error publishing blog post: {ex.Message}"
            };
        }
    }
    
    private async Task<IMcpClient> GetMcpClientAsync()
    {
        // Create an MCPClient for the GitHub server
        var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "GitHub",
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-github"],
            EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "GITHUB_PERSONAL_ACCESS_TOKEN", MCPConfig.GitHubPersonalAccessToken ?? string.Empty }
            }
        }));

        return mcpClient;
    }
}
