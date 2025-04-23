using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Plugins;

public sealed class BlogPostPlugin
{
    private readonly ILogger _logger;
    
    public BlogPostPlugin(ILogger logger)
    {
        _logger = logger;
    }

    [KernelFunction, Description("Generates a blog post from a video transcript.")]
    public async Task<VideoProcessingResponse> GenerateBlogPostAsync(
        [Description("The video transcript to generate a blog post from.")]
        string transcript,
        Kernel kernel)
    {
        _logger.LogInformation("Generating blog post from transcript: {Transcript}", transcript);
        
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a professional content writer that creates engaging blog posts from video transcripts.");
        chat.AddUserMessage($"Please create a blog post based on this transcript:\n\nTranscript:\n{transcript}\n");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        var blogPost = response.Content;
        _logger.LogInformation("Generated blog post: {BlogPost}", blogPost);
        
        return new VideoProcessingResponse
        {
            Status = "Completed",
            Transcript = transcript,
            BlogPost = blogPost!
        };
    }
} 