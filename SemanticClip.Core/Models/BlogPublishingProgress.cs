using System.Text.Json.Serialization;

namespace SemanticClip.Core.Models;

public class BlogPublishingProgress
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
    public string? Result { get; set; }
}
