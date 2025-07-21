using Microsoft.Extensions.Logging;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;
using System.Collections.Concurrent;

namespace SemanticClip.Services.Services;

/// <summary>
/// In-memory job tracking service for video processing jobs.
/// For production scenarios, consider using Redis, Azure Service Bus, or Azure Storage for persistence.
/// </summary>
public class InMemoryJobTrackingService : IJobTrackingService
{
    private readonly ConcurrentDictionary<string, VideoProcessingJob> _jobs = new();
    private readonly ILogger<InMemoryJobTrackingService> _logger;
    private readonly Timer _cleanupTimer;

    public InMemoryJobTrackingService(ILogger<InMemoryJobTrackingService> logger)
    {
        _logger = logger;
        
        // Run cleanup every hour to remove jobs older than 24 hours
        _cleanupTimer = new Timer(
            callback: _ => CleanupOldJobs(TimeSpan.FromHours(24)),
            state: null,
            dueTime: TimeSpan.FromHours(1),
            period: TimeSpan.FromHours(1)
        );
    }

    public string CreateJob(VideoProcessingRequest request)
    {
        var job = new VideoProcessingJob
        {
            Request = request,
            Progress = new VideoProcessingProgress
            {
                Status = "Pending",
                Percentage = 0,
                CurrentOperation = "Job created, waiting to start..."
            },
            Status = JobStatus.Pending
        };

        _jobs.TryAdd(job.Id, job);
        _logger.LogInformation("Created new video processing job {JobId}", job.Id);
        
        return job.Id;
    }

    public VideoProcessingJob? GetJob(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public void UpdateJobProgress(string jobId, VideoProcessingProgress progress)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Progress = progress;
            job.LastUpdated = DateTime.UtcNow;
            job.Status = progress.Status.ToLower() switch
            {
                "completed" => JobStatus.Completed,
                "failed" or "error" => JobStatus.Failed,
                _ => JobStatus.Processing
            };

            _logger.LogDebug("Updated job {JobId} progress: {Status} - {Operation} ({Percentage}%)", 
                jobId, progress.Status, progress.CurrentOperation, progress.Percentage);
        }
        else
        {
            _logger.LogWarning("Attempted to update progress for non-existent job {JobId}", jobId);
        }
    }

    public void CompleteJob(string jobId, VideoProcessingResponse result)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Result = result;
            job.Status = JobStatus.Completed;
            job.Progress.Status = "Completed";
            job.Progress.Percentage = 100;
            job.Progress.CurrentOperation = "Video processing completed successfully";
            job.Progress.Result = result;
            job.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation("Completed job {JobId}", jobId);
        }
        else
        {
            _logger.LogWarning("Attempted to complete non-existent job {JobId}", jobId);
        }
    }

    public void FailJob(string jobId, string error)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = JobStatus.Failed;
            job.Progress.Status = "Failed";
            job.Progress.Error = error;
            job.Progress.CurrentOperation = "Video processing failed";
            job.LastUpdated = DateTime.UtcNow;

            _logger.LogError("Failed job {JobId}: {Error}", jobId, error);
        }
        else
        {
            _logger.LogWarning("Attempted to fail non-existent job {JobId}", jobId);
        }
    }

    public void CancelJob(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.CancellationTokenSource.Cancel();
            job.Status = JobStatus.Cancelled;
            job.Progress.Status = "Cancelled";
            job.Progress.CurrentOperation = "Video processing was cancelled";
            job.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation("Cancelled job {JobId}", jobId);
        }
        else
        {
            _logger.LogWarning("Attempted to cancel non-existent job {JobId}", jobId);
        }
    }

    public void CleanupOldJobs(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var jobsToRemove = _jobs.Values
            .Where(job => job.LastUpdated < cutoffTime && 
                         (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed || job.Status == JobStatus.Cancelled))
            .Select(job => job.Id)
            .ToList();

        foreach (var jobId in jobsToRemove)
        {
            if (_jobs.TryRemove(jobId, out var removedJob))
            {
                removedJob.CancellationTokenSource.Dispose();
                _logger.LogDebug("Cleaned up old job {JobId}", jobId);
            }
        }

        if (jobsToRemove.Any())
        {
            _logger.LogInformation("Cleaned up {Count} old jobs", jobsToRemove.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        
        // Cancel and dispose all active jobs
        foreach (var job in _jobs.Values)
        {
            job.CancellationTokenSource.Cancel();
            job.CancellationTokenSource.Dispose();
        }
        
        _jobs.Clear();
    }
}
