using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using OpenAI;
using Azure;
using Azure.AI.OpenAI;
using SemanticClip.Core.Models;
using SemanticClip.Services.Utilities;
using System.Net.Http.Headers;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace SemanticClip.Services.Executors;

/// <summary>
/// Publishes blog post to GitHub using Model Context Protocol and OpenAI SDK
/// </summary>
public sealed class PublishBlogPostExecutor : Executor<BlogPostPublishRequest, BlogPublishingResponse>
{
    private readonly ILogger<PublishBlogPostExecutor> _logger;
    private readonly IConfiguration _configuration;
    
    public PublishBlogPostExecutor(
        ILogger<PublishBlogPostExecutor> logger,
        IConfiguration configuration) 
        : base("PublishBlogPostExecutor")
    {
        _logger = logger;
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
            _logger.LogInformation("Creating MCP client for GitHub integration");
            
            // Create MCP client for GitHub integration
            await using var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "GitHub",
                Command = "npx",
                Arguments = ["-y", "@modelcontextprotocol/server-github"],
                EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "GITHUB_PERSONAL_ACCESS_TOKEN", MCPConfig.GitHubPersonalAccessToken ?? string.Empty }
                }
            }));
            
            // Get MCP tools
            var mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            _logger.LogInformation("Found {ToolCount} MCP tools from GitHub server", mcpTools.Count);
            
            // Build instructions with commit message and content
            var instructions = $"{request.CommitMessage}\n\ncontent:{request.BlogPost}";
            
            // Create AI agent with MCP tools using OpenAI SDK client
            _logger.LogInformation("Creating AI agent for blog post publishing");
            var endpoint = _configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
            var apiKey = _configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured");
            var chatModel = _configuration["AzureOpenAI:ChatModel"] ?? _configuration["AzureOpenAI:ContentDeploymentName"] ?? "gpt-4o";
            
            AIAgent agent = new AzureOpenAIClient(
                new Uri(endpoint),
                new Azure.AzureKeyCredential(apiKey))
                 .GetChatClient(chatModel)
                 .AsIChatClient()
                 .CreateAIAgent(instructions: "You are a blog publishing assistant. Use the available GitHub tools to publish blog posts. Create or update markdown files in the repository as needed.",
                    tools: [.. mcpTools.Cast<AITool>()]);
            
            // Run the agent - it handles the agentic loop automatically
            _logger.LogInformation("Running AI agent to publish blog post");
            var response = await agent.RunAsync(instructions);
            
            _logger.LogInformation("Blog post publishing completed successfully");
            
            return new BlogPublishingResponse
            {
                Success = true,
                Message = "Blog post published successfully",
                Result = response.Text
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
}
