using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace SemanticClip.Core.Models;

public class VideoProcessingProgress
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }

    [JsonPropertyName("currentOperation")]
    public string CurrentOperation { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("result")]
    public VideoProcessingResponse? Result { get; set; }
} 