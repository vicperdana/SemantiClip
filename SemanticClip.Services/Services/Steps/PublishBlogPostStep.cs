using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using SemanticClip.Core.Models;
using SemanticClip.Services.Plugins;
using AgentThread = Microsoft.SemanticKernel.Agents.AgentThread;

namespace SemanticClip.Services.Steps;


/// <summary> Publishes the blog post to GitHub using ModelContextProtocol client with GitHub as server. </summary>
public class PublishBlogPostStep : KernelProcessStep
{
    private readonly ILogger<PublishBlogPostStep> _logger = new LoggerFactory().CreateLogger<PublishBlogPostStep>();

    private string _finalblogState = "";
    
    /*public override ValueTask ActivateAsync(KernelProcessStepState<String> state)
    {
        _finalblogState = state.State;
        return ValueTask.CompletedTask;
    }*/
    public static class Functions
    {
        public const string PublishBlogPost = nameof(PublishBlogPost);
    }
    
    private ChatCompletionAgent CreateAgentWithPlugin(
        Kernel kernel,
        KernelPlugin plugin,
        string? instructions,
        string? name, IList<McpClientTool> mcpTools)
    {
        // Create a kernel and register the MCP tools as kernel functions
        kernel.Plugins.AddFromFunctions("GitHub", mcpTools.Select(aiFunction => aiFunction.AsKernelFunction()));
        ChatCompletionAgent agent = new()
        {
            Instructions = instructions,
            Name = name,
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
        };

        agent.Kernel.Plugins.Add(plugin);
        return agent;
    }

    
    [KernelFunction(Functions.PublishBlogPost)]
    public async Task<string> PublishBlogPostAsync(string finalblogState, Kernel kernel, KernelProcessStepContext context)
    {
        _logger.LogInformation("Starting blog post publishing process");
         _finalblogState = finalblogState;
        
        try
        {
            // Get a list of MCP tools
            var mcpClient = await GetMcpClientAsync();
            var mcpTools = await mcpClient.ListToolsAsync();
            
            // Use Semantic Kernel to create an agent and publish the blog post
            var publishBlogPlugin = new PublishBlogPlugin(_logger);
            var plugin = KernelPluginFactory.CreateFromObject(publishBlogPlugin);
            string instructions = "Answer questions about GitHub repositories.";
            var agent = CreateAgentWithPlugin(kernel, plugin, instructions, "PublishBlogPost", mcpTools);
            var thread = new ChatHistoryAgentThread();

            var result = await InvokeAgentAsync(agent, thread, instructions);

            // Update the state with the published blog post
            this._finalblogState = result.ToString();
            return this._finalblogState;
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
        var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
        {
            Name = "GitHub",
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-github"],
        }));

        return mcpClient;
    }
    
    private async Task<string> InvokeAgentAsync(ChatCompletionAgent agent, AgentThread thread, string input)
    {
        //var message = new ChatMessageContent(AuthorRole.User, input);
        var message = new ChatMessageContent(AuthorRole.User, "Summarize the last four commits to the vicperdana/semanticlip repository using the list_commits function.");
        string? lastResponse = null;

        await foreach (var response in agent.InvokeAsync(message, thread))
        {
            lastResponse = response.Message.Content;
            _logger.LogInformation("Agent response: {Response}", lastResponse);
        }

        return lastResponse ?? string.Empty;
    }
    
}