using SemanticClip.Core.Models;

namespace SemanticClip.Core.Interfaces;

public interface IVideoProcessingService
{
    Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request);
    void SetProgressCallback(Action<VideoProcessingProgress>? callback);
}
