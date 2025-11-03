namespace SemanticClip.Services.Services;

public interface IAudioTranscriptionService
{
    Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default);
}
