using SemanticClip.Core.Models;

namespace SemanticClip.Core.Services;

public interface IVideoProcessingService
{
    Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request);
    Task<string> TranscribeVideoAsync(string videoPath);
    Task<List<Chapter>> GenerateChaptersAsync(string transcript);
    Task<string> GenerateBlogPostAsync(string transcript, List<Chapter> chapters);
    void SetProgressCallback(Action<VideoProcessingProgress>? callback);
} 