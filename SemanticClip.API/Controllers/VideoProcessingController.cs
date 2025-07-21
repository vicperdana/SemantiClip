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
}