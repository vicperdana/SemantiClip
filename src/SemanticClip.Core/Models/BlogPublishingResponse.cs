using System;

namespace SemanticClip.Core.Models;

public class BlogPublishingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Result { get; set; }
}
