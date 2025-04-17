namespace SemanticClip.Core.Models;

public class VideoProcessingResponse
{
    public string Transcript { get; set; } = string.Empty;
    public string BlogPost { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class Chapter
{
    public string Title { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Summary { get; set; } = string.Empty;
} 