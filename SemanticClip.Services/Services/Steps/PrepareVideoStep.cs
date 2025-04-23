using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Steps;

public class PrepareVideoStep : KernelProcessStep
{
    public static class Functions
    {
        public const string PrepareVideo = nameof(PrepareVideo);
    }
    internal string? _videoPath;

    [KernelFunction(Functions.PrepareVideo)]
    public async Task<string> PrepareVideoAsync(VideoProcessingRequest request, KernelProcessStepContext context)
    {
        if (string.IsNullOrEmpty(request.FileContent))
        {
            throw new ArgumentException("File content must be provided");
        }

        var tempPath = Path.GetTempFileName();
        var fileExtension = Path.GetExtension(request.FileName);
        var finalPath = Path.ChangeExtension(tempPath, fileExtension);
        File.Move(tempPath, finalPath);
        
        var fileBytes = Convert.FromBase64String(request.FileContent);
        await File.WriteAllBytesAsync(finalPath, fileBytes);
        _videoPath = finalPath;
        
        await context.EmitEventAsync(new KernelProcessEvent{Id = "VideoPrepared", Data = _videoPath});
        return _videoPath;
    }
} 