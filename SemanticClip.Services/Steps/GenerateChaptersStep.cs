using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Process;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Steps;

#pragma warning disable SKEXP0080
public class GenerateChaptersStep : KernelProcessStep
{
    public static class Functions
    {
        public const string GenerateChaptersStep = nameof(GenerateChaptersStep);
    }
    internal List<Chapter> _chapters = new();

    [KernelFunction(Functions.GenerateChaptersStep)]
    public async Task<List<Chapter>> GenerateChaptersAsync(string transcript, ILogger logger, Action<VideoProcessingProgress> progressCallback, Kernel kernel, KernelProcessStepContext context)
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
        
        UpdateProgress("Generating", 60, "Generating chapters");
        
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a helpful assistant that analyzes video transcripts and suggests logical chapter divisions with timestamps.");
        chat.AddUserMessage($"Please analyze this transcript and suggest chapters with timestamps:\n\n{transcript}");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        _chapters = ParseChaptersFromResponse(response.Content);
        
        await context.EmitEventAsync("ChaptersGenerated", _chapters);
        return _chapters;
    }
    
    private List<Chapter> ParseChaptersFromResponse(string response)
    {
        var chapters = new List<Chapter>();
        var lines = response.Split('\n');

        foreach (var line in lines)
        {
            if (line.Contains(":"))
            {
                var parts = line.Split(':');
                if (parts.Length >= 2)
                {
                    var timePart = parts[0].Trim();
                    var titlePart = parts[1].Trim();

                    if (TimeSpan.TryParse(timePart, out var time))
                    {
                        chapters.Add(new Chapter
                        {
                            Title = titlePart,
                            StartTime = time,
                            EndTime = time.Add(TimeSpan.FromMinutes(5)) // Default 5-minute chapter length
                        });
                    }
                }
            }
        }

        return chapters;
    }
} 