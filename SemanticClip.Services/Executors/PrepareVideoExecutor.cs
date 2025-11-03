using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Executors;

/// <summary>
/// First executor: prepares video file for processing by validating and saving to disk
/// </summary>
public sealed class PrepareVideoExecutor : Executor<VideoProcessingRequest, string>
{
    private readonly ILogger<PrepareVideoExecutor> _logger;
    
    public PrepareVideoExecutor(ILogger<PrepareVideoExecutor> logger) 
        : base("PrepareVideoExecutor")
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Validates the video processing request and saves the video file to disk
    /// </summary>
    /// <param name="request">The video processing request containing file data</param>
    /// <param name="context">Workflow context for accessing services</param>
    /// <returns>Path to the prepared video file</returns>
    public override async ValueTask<string> HandleAsync(
        VideoProcessingRequest request, 
        IWorkflowContext context)
    {
        try
        {
            _logger.LogInformation("Starting video preparation for file: {FileName}", request.FileName);
            
            // Check if we already have a temp file path
            if (!string.IsNullOrEmpty(request.TempFilePath) && File.Exists(request.TempFilePath))
            {
                _logger.LogInformation("Using pre-uploaded temp file at {Path}", request.TempFilePath);
                return request.TempFilePath;
            }
            
            // Validate file content
            if (string.IsNullOrEmpty(request.FileContent))
            {
                _logger.LogError("File content is null or empty, and no TempFilePath provided");
                throw new ArgumentException("File content must be provided");
            }

            // Create temporary file with proper extension
            _logger.LogInformation("Creating temporary file for video content");
            var tempPath = Path.GetTempFileName();
            var fileExtension = Path.GetExtension(request.FileName);
            var finalPath = Path.ChangeExtension(tempPath, fileExtension);
            
            // Move temp file to have correct extension
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }
            File.Move(tempPath, finalPath);
            
            // Decode base64 content and write to file
            _logger.LogInformation("Decoding base64 content and writing to file: {FilePath}", finalPath);
            var fileBytes = Convert.FromBase64String(request.FileContent);
            await File.WriteAllBytesAsync(finalPath, fileBytes);
            
            _logger.LogInformation("Video preparation completed successfully. File size: {FileSize} bytes", fileBytes.Length);
            
            // Return the file path - it will automatically flow to the next executor
            return finalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PrepareVideoExecutor: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}
