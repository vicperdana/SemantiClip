using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;

namespace SemanticClip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporarily disabled for simple auth
public class VideoProcessingController : ControllerBase
{
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IJobTrackingService _jobTrackingService;
    private readonly ILogger<VideoProcessingController> _logger;

    public VideoProcessingController(
        IVideoProcessingService videoProcessingService,
        IJobTrackingService jobTrackingService,
        ILogger<VideoProcessingController> logger)
    {
        _videoProcessingService = videoProcessingService;
        _jobTrackingService = jobTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// Starts video processing and returns a job ID for tracking progress
    /// </summary>
    [HttpPost("process")]
    public IActionResult StartVideoProcessing([FromBody] VideoProcessingRequest request)
    {
        try
        {
            _logger.LogInformation("Received video processing request for file: {FileName}", request?.FileName);
            
            if (request == null)
            {
                _logger.LogWarning("Received null request");
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrEmpty(request.FileContent) || string.IsNullOrEmpty(request.FileName))
            {
                _logger.LogWarning("Invalid request - missing file content or filename. FileName: {FileName}, HasFileContent: {HasContent}", 
                    request.FileName, !string.IsNullOrEmpty(request.FileContent));
                return BadRequest("File content and filename are required.");
            }

            _logger.LogInformation("Processing valid request for file: {FileName}, Size: {Size} bytes", 
                request.FileName, request.FileContent?.Length ?? 0);

            // Create a job for tracking
            var jobId = _jobTrackingService.CreateJob(request);
            _logger.LogInformation("Created video processing job {JobId} for file {FileName}", jobId, request.FileName);

            // Start processing in the background
            _ = Task.Run(async () => await ProcessVideoInBackgroundAsync(jobId, request));

            var response = new JobStartResponse
            {
                JobId = jobId,
                Status = "Started",
                Message = "Video processing has been started. Use the job ID to check progress."
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting video processing");
            return StatusCode(500, new { Error = "Failed to start video processing", Details = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current status and progress of a video processing job
    /// </summary>
    [HttpGet("status/{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        try
        {
            var job = _jobTrackingService.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new { Error = "Job not found", JobId = jobId });
            }

            return Ok(new
            {
                JobId = jobId,
                Status = job.Status.ToString(),
                Progress = job.Progress,
                Result = job.Result,
                CreatedAt = job.CreatedAt,
                LastUpdated = job.LastUpdated
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status for {JobId}", jobId);
            return StatusCode(500, new { Error = "Failed to get job status", Details = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a video processing job
    /// </summary>
    [HttpPost("cancel/{jobId}")]
    public IActionResult CancelJob(string jobId)
    {
        try
        {
            var job = _jobTrackingService.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new { Error = "Job not found", JobId = jobId });
            }

            if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed)
            {
                return BadRequest(new { Error = "Cannot cancel a job that is already completed or failed", JobId = jobId });
            }

            _jobTrackingService.CancelJob(jobId);
            _logger.LogInformation("Cancelled video processing job {JobId}", jobId);

            return Ok(new { JobId = jobId, Status = "Cancelled", Message = "Job has been cancelled successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
            return StatusCode(500, new { Error = "Failed to cancel job", Details = ex.Message });
        }
    }

    /// <summary>
    /// Background method to process video and update job progress
    /// </summary>
    private async Task ProcessVideoInBackgroundAsync(string jobId, VideoProcessingRequest request)
    {
        try
        {
            _logger.LogInformation("Starting background processing for job {JobId}", jobId);

            // Get the job for cancellation token
            var job = _jobTrackingService.GetJob(jobId);
            if (job == null)
            {
                _logger.LogError("Job {JobId} not found when starting background processing", jobId);
                return;
            }

            // Setup progress callback
            Action<VideoProcessingProgress> progressCallback = (progress) =>
            {
                _jobTrackingService.UpdateJobProgress(jobId, progress);
            };

            // Process the video with progress updates
            var result = await _videoProcessingService.ProcessVideoAsync(request, progressCallback);

            // Mark job as completed
            _jobTrackingService.CompleteJob(jobId, result);
            _logger.LogInformation("Completed video processing job {JobId}", jobId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Video processing job {JobId} was cancelled", jobId);
            _jobTrackingService.UpdateJobProgress(jobId, new VideoProcessingProgress
            {
                Status = "Cancelled",
                CurrentOperation = "Processing was cancelled by user",
                Percentage = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video for job {JobId}", jobId);
            _jobTrackingService.FailJob(jobId, ex.Message);
        }
    }

    /// <summary>
    /// Accepts large video uploads via multipart form and starts a processing job.
    /// </summary>
    [HttpPost("upload-multipart")]
    [RequestSizeLimit(1_073_741_824)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824)]
    public async Task<IActionResult> UploadMultipartAsync([FromForm] IFormFile videoFile, [FromForm] string? title, [FromForm] string? description)
    {
        _logger.LogInformation("UploadMultipartAsync called with videoFile: {HasFile}, title: {Title}, description: {Description}", 
            videoFile != null, title, description);
            
        if (videoFile == null || videoFile.Length == 0)
        {
            _logger.LogWarning("No file uploaded or file is empty");
            return BadRequest("No file uploaded");
        }

        _logger.LogInformation("File received: {FileName}, Size: {Size} bytes, ContentType: {ContentType}", 
            videoFile.FileName, videoFile.Length, videoFile.ContentType);

        // Optional: check configured max size if present
        var maxSizeConfig = HttpContext.RequestServices.GetService<IConfiguration>()?.GetValue<long?>("FileUpload:MaxRequestBodySizeInBytes");
        if (maxSizeConfig.HasValue && videoFile.Length > maxSizeConfig.Value)
        {
            _logger.LogWarning("File exceeds configured limit: {FileSize} > {MaxSize}", videoFile.Length, maxSizeConfig.Value);
            return BadRequest($"File exceeds configured limit of {maxSizeConfig.Value} bytes");
        }

        try
        {
            // Persist to temp and pass path through request
            var tempFile = Path.GetTempFileName();
            var finalPath = Path.ChangeExtension(tempFile, Path.GetExtension(videoFile.FileName));
            System.IO.File.Move(tempFile, finalPath);
            
            _logger.LogInformation("Creating temp file at: {TempPath}", finalPath);
            
            await using (var fs = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await videoFile.CopyToAsync(fs);
            }

            _logger.LogInformation("File saved to temp location: {TempPath}, Size: {Size}", finalPath, new FileInfo(finalPath).Length);

            var request = new VideoProcessingRequest
            {
                FileName = videoFile.FileName,
                Title = title,
                Description = description,
                TempFilePath = finalPath
            };

            var jobId = _jobTrackingService.CreateJob(request);
            _logger.LogInformation("Created job {JobId} for file {FileName}", jobId, videoFile.FileName);
            
            _ = Task.Run(async () => await ProcessVideoInBackgroundAsync(jobId, request));

            return Ok(new JobStartResponse
            {
                JobId = jobId,
                Status = "Started",
                Message = "Video upload received; processing job started.",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing multipart upload");
            return StatusCode(500, new { Error = "Internal server error", Details = ex.Message });
        }
    }
}