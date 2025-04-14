namespace SemanticClip.Core.Models;

public class VideoProcessingRequest
{
    public string YouTubeUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileContent { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
} 