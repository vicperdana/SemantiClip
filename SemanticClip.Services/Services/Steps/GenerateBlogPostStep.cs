using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Core.Models;
using SemanticClip.Services.Plugins;

namespace SemanticClip.Services.Steps;

#pragma warning disable SKEXP0080
public class GenerateBlogPostStep : KernelProcessStep<VideoProcessingResponse>
{
    ILogger<GenerateBlogPostStep> _logger = new LoggerFactory().CreateLogger<GenerateBlogPostStep>();
    private VideoProcessingResponse? _state;
    

    public override ValueTask ActivateAsync(KernelProcessStepState<VideoProcessingResponse> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }

    private ChatCompletionAgent CreateAgentWithPlugin(
        Kernel kernel,
        KernelPlugin plugin,
        string? instructions = null,
        string? name = null)
    {
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
    
    public static class Functions
    {
        public const string GenerateBlogPost = nameof(GenerateBlogPost);
    }

    [KernelFunction(Functions.GenerateBlogPost)]
    public async Task<VideoProcessingResponse> GenerateBlogPostAsync(string transcript, Kernel kernel, KernelProcessStepContext context)
    {
        _logger.LogInformation("Starting blog post generation process");
        
        // Create the blog post plugin directly
        var blogPostPlugin = new BlogPostPlugin(_logger);
        var plugin = KernelPluginFactory.CreateFromObject(blogPostPlugin);

        // Create the agent
        var agent = CreateAgentWithPlugin(
            kernel: kernel,
            plugin: plugin,
            instructions: "Generate a blog post from the provided video transcript.",
            name: "BlogPostGenerator");

        // Create the chat history thread
        var thread = new ChatHistoryAgentThread();

        // Invoke the agent with the transcript
        var result = await InvokeAgentAsync(agent, thread, transcript);
        
        // Update the state with the generated blog post
        _state!.Status = "Completed";
        _state!.Transcript = transcript;
        _state!.BlogPost = result;
        
        await context.EmitEventAsync("Completed", _state);
        
        return _state;
    }
    
    private async Task<string> InvokeAgentAsync(ChatCompletionAgent agent, AgentThread thread, string input)
    {
        var message = new ChatMessageContent(AuthorRole.User, input);
        string? lastResponse = null;

        await foreach (var response in agent.InvokeAsync(message, thread))
        {
            lastResponse = response.Message.Content;
            _logger.LogInformation("Agent response: {Response}", lastResponse);
        }

        return lastResponse ?? string.Empty;
    }
} 