namespace SemanticClip.Core.Models;

public class VideoProcessingJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public VideoProcessingRequest Request { get; set; } = new();
    public VideoProcessingProgress Progress { get; set; } = new();
    public VideoProcessingResponse? Result { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();
}

public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}
