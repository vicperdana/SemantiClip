using SemanticClip.Core.Models;

namespace SemanticClip.Core.Interfaces;

public interface IJobTrackingService
{
    /// <summary>
    /// Creates a new video processing job and returns the job ID
    /// </summary>
    string CreateJob(VideoProcessingRequest request);
    
    /// <summary>
    /// Gets the current status and progress of a job
    /// </summary>
    VideoProcessingJob? GetJob(string jobId);
    
    /// <summary>
    /// Updates the progress of a job
    /// </summary>
    void UpdateJobProgress(string jobId, VideoProcessingProgress progress);
    
    /// <summary>
    /// Marks a job as completed with results
    /// </summary>
    void CompleteJob(string jobId, VideoProcessingResponse result);
    
    /// <summary>
    /// Marks a job as failed with error details
    /// </summary>
    void FailJob(string jobId, string error);
    
    /// <summary>
    /// Cancels a job
    /// </summary>
    void CancelJob(string jobId);
    
    /// <summary>
    /// Removes old completed jobs (cleanup)
    /// </summary>
    void CleanupOldJobs(TimeSpan maxAge);
}
