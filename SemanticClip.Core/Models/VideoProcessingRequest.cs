namespace SemanticClip.Core.Models;

public class VideoProcessingRequest
{
    public string? FileName { get; set; }
    public string? FileContent { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? TempFilePath { get; set; } // optional server-side path for large uploads
}