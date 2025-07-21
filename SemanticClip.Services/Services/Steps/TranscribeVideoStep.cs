using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.Process;
using System.Text;
using Xabe.FFmpeg;
using SemanticClip.Services.Utils;

namespace SemanticClip.Services.Steps;

public class TranscribeVideoStep : KernelProcessStep
{
    private string _transcript = "";
    private ILogger _logger = new LoggerFactory().CreateLogger<TranscribeVideoStep>();

    public static class Functions
    {
        public const string TranscribeVideo = nameof(TranscribeVideoStep);
    }

    [KernelFunction(Functions.TranscribeVideo)]
    public async Task<string> TranscribeVideoAsync(string videoPath, Kernel kernel, KernelProcessStepContext context)
    {
        // Ensure FFMpeg is configured and binaries are available
        await FFMpegConfiguration.EnsureFFMpegAsync(_logger);
        
        _logger.LogInformation("Extracting audio from video: {VideoPath}", videoPath);
        string outputAudioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
        
        try
        {
            await ExtractAudioFromVideoAsync(videoPath, outputAudioPath);
            _transcript = await TranscribeAudioFileAsync(outputAudioPath, kernel);
            await context.EmitEventAsync("TranscriptionComplete", _transcript);
            return _transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio extraction or transcription");
            throw new Exception($"Audio processing failed: {ex.Message}", ex);
        }
        finally
        {
            CleanupTemporaryFile(outputAudioPath);
        }
    }

    private async Task ExtractAudioFromVideoAsync(string videoPath, string outputAudioPath)
    {
        _logger.LogInformation("Extracting audio from video using Xabe.FFmpeg with 2x speed: {VideoPath}", videoPath);
        
        try
        {
            // Use Xabe.FFmpeg to extract audio from video
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
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

            await conversion.Start();

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

    private async Task<string> TranscribeAudioFileAsync(string audioPath, Kernel kernel)
    {
        _logger.LogInformation("Transcribing audio: {AudioPath}", audioPath);
        
        var audioToTextService = kernel.GetRequiredService<IAudioToTextService>();
        
        using var audioFileStream = new FileStream(audioPath, FileMode.Open, FileAccess.Read);
        var audioFileBinaryData = await BinaryData.FromStreamAsync(audioFileStream);
        
        AudioContent audioContent = new(audioFileBinaryData, mimeType: null);
        
        var result = await audioToTextService.GetTextContentAsync(audioContent);
        _logger.LogInformation("Transcription completed successfully");
        
        return result.Text ?? throw new InvalidOperationException("Transcription returned null result");
    }

    private void CleanupTemporaryFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try 
            { 
                File.Delete(filePath);
                _logger.LogInformation("Deleted temporary audio file: {AudioPath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary audio file: {AudioPath}", filePath);
            }
        }
    }
}
