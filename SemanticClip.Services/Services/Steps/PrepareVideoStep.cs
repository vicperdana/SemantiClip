using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using Microsoft.Extensions.Logging;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Steps;

public class PrepareVideoStep : KernelProcessStep
{
    public static class Functions
    {
        public const string PrepareVideo = nameof(PrepareVideo);
    }
    internal string? _videoPath;
    private readonly ILogger _logger = new LoggerFactory().CreateLogger<PrepareVideoStep>();

    [KernelFunction(Functions.PrepareVideo)]
    public async Task<string> PrepareVideoAsync(VideoProcessingRequest request, KernelProcessStepContext context)
    {
        try
        {
            _logger.LogInformation("Starting PrepareVideoStep for file: {FileName}", request.FileName);
            
            if (string.IsNullOrEmpty(request.FileContent))
            {
                _logger.LogError("File content is null or empty");
                throw new ArgumentException("File content must be provided");
            }

            _logger.LogInformation("Creating temporary file for video content");
            var tempPath = Path.GetTempFileName();
            var fileExtension = Path.GetExtension(request.FileName);
            var finalPath = Path.ChangeExtension(tempPath, fileExtension);
            File.Move(tempPath, finalPath);
            
            _logger.LogInformation("Decoding base64 content and writing to file: {FilePath}", finalPath);
            var fileBytes = Convert.FromBase64String(request.FileContent);
            await File.WriteAllBytesAsync(finalPath, fileBytes);
            _videoPath = finalPath;
            
            _logger.LogInformation("Video preparation completed successfully. File size: {FileSize} bytes", fileBytes.Length);
            await context.EmitEventAsync(new KernelProcessEvent{Id = "VideoPrepared", Data = _videoPath});
            
            return _videoPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PrepareVideoStep: {ErrorMessage}", ex.Message);
            throw;
        }
    }
} 