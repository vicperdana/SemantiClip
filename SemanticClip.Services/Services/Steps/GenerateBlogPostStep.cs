using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Process;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Steps;

#pragma warning disable SKEXP0080
public class GenerateBlogPostStep : KernelProcessStep<VideoProcessingResponse>
{   
    
    public static class Functions
    {
        public const string GenerateBlogPost = nameof(GenerateBlogPost);
    }
    
    public override ValueTask ActivateAsync(KernelProcessStepState<VideoProcessingResponse> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }

    internal VideoProcessingResponse? _state;
    ILogger _logger = new LoggerFactory().CreateLogger<GenerateBlogPostStep>();

    [KernelFunction(Functions.GenerateBlogPost)]
    public async Task<VideoProcessingResponse> GenerateBlogPostAsync(string transcript, Kernel kernel, KernelProcessStepContext context)
    {
        _logger.LogInformation("Generating blog post from transcript: {Transcript}", transcript);
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a professional content writer that creates engaging blog posts from video transcripts.");
        chat.AddUserMessage($"Please create a blog post based on this transcript:\n\nTranscript:\n{transcript}\n");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        var blogPost = response.Content;
        _logger.LogInformation("Generated blog post: {BlogPost}", blogPost);
        
        // Create the response object
        this._state!.Status = "Completed";
        this._state!.Transcript = transcript;
        this._state!.BlogPost = blogPost;
        
        await context.EmitEventAsync("Completed", _state);
        
        return _state;
    }
} 