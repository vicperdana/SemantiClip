using Microsoft.AspNetCore.Http;

namespace SemanticClip.Core.Models;

public class VideoProcessingRequest
{
    public string? YouTubeUrl { get; set; }
    public IFormFile? VideoFile { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
} 