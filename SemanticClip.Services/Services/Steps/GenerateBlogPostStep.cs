using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Process;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Steps;

#pragma warning disable SKEXP0080
public class GenerateBlogPostStep : KernelProcessStep
{   
    public static class Functions
    {
        public const string GenerateBlogPostStep = nameof(GenerateBlogPostStep);
    }
    internal string _blogPost = "";

    [KernelFunction(Functions.GenerateBlogPostStep)]
    public async Task<string> GenerateBlogPostAsync(string transcript, List<Chapter> chapters, ILogger logger, Action<VideoProcessingProgress> progressCallback, Kernel kernel, KernelProcessStepContext context)
    {
        void UpdateProgress(string status, int percentage, string currentOperation = "", string? error = null)
        {
            progressCallback?.Invoke(new VideoProcessingProgress
            {
                Status = status,
                Percentage = percentage,
                CurrentOperation = currentOperation,
                Error = error
            });
        }
        
        UpdateProgress("Creating", 80, "Creating blog post");
        
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a professional content writer that creates engaging blog posts from video transcripts.");
        chat.AddUserMessage($"Please create a blog post based on this transcript and chapters:\n\nTranscript:\n{transcript}\n\nChapters:\n{string.Join("\n", chapters.Select(c => $"{c.Title} ({c.StartTime:hh\\:mm\\:ss} - {c.EndTime:hh\\:mm\\:ss})"))}");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        _blogPost = response.Content;
        
        await context.EmitEventAsync("BlogPostGenerated", _blogPost);
        return _blogPost;
    }
} 