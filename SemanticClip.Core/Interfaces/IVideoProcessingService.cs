using SemanticClip.Core.Models;

namespace SemanticClip.Core.Interfaces;

public interface IVideoProcessingService
{
    Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request);
    Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request, Action<VideoProcessingProgress>? progressCallback = null);
    void SetProgressCallback(Action<VideoProcessingProgress>? callback);
}
