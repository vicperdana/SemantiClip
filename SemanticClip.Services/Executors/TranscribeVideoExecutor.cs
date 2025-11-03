using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using SemanticClip.Services.Utils;
using SemanticClip.Services.Services;

namespace SemanticClip.Services.Executors;

/// <summary>
/// Second executor: extracts audio from video and transcribes using Azure OpenAI Whisper
/// </summary>
public sealed class TranscribeVideoExecutor : Executor<string, string>
{
    private readonly ILogger<TranscribeVideoExecutor> _logger;
    private readonly IAudioTranscriptionService _audioService;
    
    public TranscribeVideoExecutor(
        ILogger<TranscribeVideoExecutor> logger,
        IAudioTranscriptionService audioService) 
        : base("TranscribeVideoExecutor")
    {
        _logger = logger;
        _audioService = audioService;
    }
    
    /// <summary>
    /// Extracts audio from video file and transcribes it to text
    /// </summary>
    /// <param name="videoPath">Path to the video file</param>
    /// <param name="context">Workflow context for accessing services</param>
    /// <returns>Transcribed text from the video's audio</returns>
    public override async ValueTask<string> HandleAsync(
        string videoPath, 
        IWorkflowContext context)
    {
        // Ensure FFMpeg is configured and binaries are available
        await FFMpegConfiguration.EnsureFFMpegAsync(_logger);
        
        _logger.LogInformation("Extracting audio from video: {VideoPath}", videoPath);
        string outputAudioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
        
        try
        {
            // Extract audio from video
            await ExtractAudioFromVideoAsync(videoPath, outputAudioPath);
            
            // Transcribe audio using injected service
            var transcript = await _audioService.TranscribeAsync(outputAudioPath, CancellationToken.None);
            
            _logger.LogInformation("Transcription completed: {Length} characters", transcript.Length);
            
            return transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio extraction or transcription");
            throw new Exception($"Audio processing failed: {ex.Message}", ex);
        }
        finally
        {
            CleanupTemporaryFile(outputAudioPath);
            CleanupTemporaryFile(videoPath);
        }
    }

    private async Task ExtractAudioFromVideoAsync(string videoPath, string outputAudioPath)
    {
        _logger.LogInformation("Extracting audio from video using Xabe.FFmpeg with 2x speed: {VideoPath}", videoPath);
        
        try
        {
            // Use Xabe.FFmpeg to extract audio from video
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath, CancellationToken.None);
            var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
            
            if (audioStream == null)
            {
                throw new InvalidOperationException("No audio stream found in the video file");
            }

            // Configure audio stream settings with 2x speed
            audioStream.SetSampleRate(16000);

            var conversion = FFmpeg.Conversions.New()
                .AddStream(audioStream)
                .AddParameter("-filter:a atempo=2.0") // Speed up audio by 2x
                .SetOutput(outputAudioPath);

            await conversion.Start(CancellationToken.None);

            if (!File.Exists(outputAudioPath))
            {
                throw new FileNotFoundException("Audio extraction did not produce the expected output file");
            }
            
            _logger.LogInformation("Successfully extracted audio to: {AudioPath}", outputAudioPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFMpegCore audio extraction failed");
            throw new Exception($"Failed to extract audio: {ex.Message}", ex);
        }
    }

    private void CleanupTemporaryFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try 
            { 
                File.Delete(filePath);
                _logger.LogInformation("Deleted temporary file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary file: {FilePath}", filePath);
            }
        }
    }
}
