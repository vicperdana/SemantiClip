using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Core.Models;
using SemanticClip.Services.Utils;

namespace SemanticClip.Services.Steps;

public class GenerateBlogPostStep : KernelProcessStep<BlogPostProcessingResponse>
{
    ILogger<GenerateBlogPostStep> _logger = new LoggerFactory().CreateLogger<GenerateBlogPostStep>();
    private BlogPostProcessingResponse? _state = new BlogPostProcessingResponse();
    
    // Alternative method to create the agent with a plugin
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
    
    
    private ChatCompletionAgent UseTemplateForChatCompletionAgent(
        Kernel kernel, string transcript)
    {
        string generateBlogPostYaml = EmbeddedResource.Read("GenerateBlogPost.yaml");
        PromptTemplateConfig templateConfig = KernelFunctionYaml.ToPromptTemplateConfig(generateBlogPostYaml);
        KernelPromptTemplateFactory templateFactory = new();
        
        ChatCompletionAgent agent = new(templateConfig, templateFactory)
        {
            Kernel = kernel,
            Arguments = new()
            {
                { "transcript", transcript }
            }
         };

        return agent;
    }
    
    public static class Functions
    {
        public const string GenerateBlogPost = nameof(GenerateBlogPost);
    }

    [KernelFunction(Functions.GenerateBlogPost)]
    public async Task<BlogPostProcessingResponse> GenerateBlogPostAsync(string transcript, Kernel kernel, KernelProcessStepContext context)
    {
        _logger.LogInformation("Starting blog post generation process");
        
        /* METHOD 1 with a plugin
        
        //Create the blog post plugin
        var blogPostPlugin = new BlogPostPlugin(_logger);
        var plugin = KernelPluginFactory.CreateFromObject(blogPostPlugin);

        //Create the agent
        var agent = CreateAgentWithPlugin(
            kernel: kernel,
            plugin: plugin,
            instructions: "Generate a blog post from the provided video transcript.",
            name: "BlogPostGenerator");
        */
        
        // METHOD 2 with a template
        //Create the agent with a template
        var agent = UseTemplateForChatCompletionAgent(
            kernel: kernel,
            transcript: transcript);

        // Create the chat history thread
        var thread = new ChatHistoryAgentThread();

        // Invoke the agent with the transcript
        var result = await InvokeAgentAsync(agent, thread, transcript);
        
        // Update the state with the generated blog post
        VideoProcessingResponse videoProcessingResponse = new VideoProcessingResponse
        {
            Transcript = transcript,
            BlogPost = result
        };
        _state!.BlogPosts.Add(result);
        _state!.VideoProcessingResponse = videoProcessingResponse;
        
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