using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using SemanticClip.Core.Models;
using SemanticClip.Services.Plugins;
using SemanticClip.Services.Utilities;
using AgentThread = Microsoft.SemanticKernel.Agents.AgentThread;

namespace SemanticClip.Services.Steps;

/// <summary> Publishes the blog post to GitHub using ModelContextProtocol client with GitHub as server. </summary>
public class PublishBlogPostStep : KernelProcessStep<BlogPublishingResponse>
{
    private readonly ILogger<PublishBlogPostStep> _logger = new LoggerFactory().CreateLogger<PublishBlogPostStep>();
    private BlogPublishingResponse? _finalblogState = new();
    //private BlogPostPublishRequest _state = new();

    public override ValueTask ActivateAsync(KernelProcessStepState<BlogPublishingResponse> state)
    {
        _finalblogState = state.State;
        return ValueTask.CompletedTask;
    }

    public static class Functions
    {
        public const string PublishBlogPost = nameof(PublishBlogPost);
    }

    private ChatCompletionAgent CreateAgentWithPlugin(
        Kernel kernel,
        KernelPlugin plugin,
        string? instructions,
        string? name,
        IList<McpClientTool>? mcpTools)
    {
        // Create a kernel and register the MCP tools as kernel functions
        if (mcpTools != null && mcpTools.Any())
        {
            kernel.Plugins.AddFromFunctions("GitHub", mcpTools.Select(aiFunction => aiFunction.AsKernelFunction()));
        }
        ChatCompletionAgent agent = new()
        {
            Instructions = instructions,
            Name = name,
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings
                { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
        };

        agent.Kernel.Plugins.Add(plugin);
        return agent;
    }


    [KernelFunction(Functions.PublishBlogPost)]
    public async Task PublishBlogPostAsync(BlogPostPublishRequest? request, Kernel kernel,
        KernelProcessStepContext context)
    {
        _logger.LogInformation("Starting blog post publishing process");
        BlogPostPublishRequest blogRequest = request ?? throw new ArgumentNullException(nameof(request));
        string? blogPostContent = blogRequest.BlogPost;
        
        if (string.IsNullOrEmpty(blogPostContent))
        {
            _logger.LogWarning("Blog post content is null or empty");
            return;
        }

        try
        {
            // Get a list of MCP tools
            var mcpClient = await GetMcpClientAsync();
            var mcpTools = await mcpClient.ListToolsAsync();

            // Use Semantic Kernel to create an agent and publish the blog post
            var publishBlogPlugin = new PublishBlogPlugin(_logger);
            var plugin = KernelPluginFactory.CreateFromObject(publishBlogPlugin);
            var instructions = request.CommitMessage +
                               " \n\n content:" + request.BlogPost; 
            var agent = CreateAgentWithPlugin(kernel, plugin, instructions, "PublishBlogPost", mcpTools);
            var thread = new ChatHistoryAgentThread();

            var result = await InvokeAgentAsync(agent, thread, instructions);

            // Update the state with the published blog post
            string PublishBlogPostComplete = nameof(PublishBlogPostComplete);
            this._finalblogState!.Message = "Blog post published successfully";
            this._finalblogState.Success = true;
            this._finalblogState.Result = result.ToString();
            await context.EmitEventAsync(new KernelProcessEvent
            {
                Id = PublishBlogPostComplete, Data = this._finalblogState, Visibility = KernelProcessEventVisibility.Public
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error evaluating blog post: {Error}", ex.Message);
            throw;
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

    private async Task<string> InvokeAgentAsync(ChatCompletionAgent agent, AgentThread thread, string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }
        
        var message = new ChatMessageContent(AuthorRole.User, input);
        string? lastResponse = string.Empty;

        await foreach (var response in agent.InvokeAsync(message, thread))
        {
            lastResponse = response.Message.Content;
            _logger.LogInformation("Agent response: {Response}", lastResponse);
        }

        return lastResponse ?? string.Empty;
    }
}