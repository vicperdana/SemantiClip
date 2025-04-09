using SemanticClip.Core.Models;

namespace SemanticClip.Core.Services;

public interface IVideoProcessingService
{
    Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request);
    Task<string> TranscribeVideoAsync(Stream videoStream);
    Task<List<Chapter>> GenerateChaptersAsync(string transcript);
    Task<string> GenerateBlogPostAsync(string transcript, List<Chapter> chapters);
} 