using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.Process;
using System.Text;

namespace SemanticClip.Services.Steps;

#pragma warning disable SKEXP0080
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
        _logger.LogInformation("Extracting audio from video: {VideoPath}", videoPath);
        
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{videoPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputAudioPath}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        
        process.OutputDataReceived += (sender, args) => {
            if (args.Data != null) outputBuilder.AppendLine(args.Data);
        };
        
        process.ErrorDataReceived += (sender, args) => {
            if (args.Data != null) errorBuilder.AppendLine(args.Data);
        };
        
        _logger.LogInformation("Running FFmpeg command");
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        
        try 
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw new Exception("Audio extraction timed out after 10 minutes");
        }
        
        if (process.ExitCode != 0)
        {
            string errorOutput = errorBuilder.ToString();
            _logger.LogError("FFmpeg error: {Error}", errorOutput);
            throw new Exception($"Failed to extract audio: {errorOutput}");
        }
        
        if (!File.Exists(outputAudioPath))
        {
            throw new FileNotFoundException("Audio extraction did not produce the expected output file");
        }
        
        _logger.LogInformation("Successfully extracted audio to: {AudioPath}", outputAudioPath);
    }

    private async Task<string> TranscribeAudioFileAsync(string audioPath, Kernel kernel)
    {
        _logger.LogInformation("Transcribing audio: {AudioPath}", audioPath);
        
        #pragma warning disable SKEXP0001
        var audioToTextService = kernel.GetRequiredService<IAudioToTextService>();
        
        using var audioFileStream = new FileStream(audioPath, FileMode.Open, FileAccess.Read);
        var audioFileBinaryData = await BinaryData.FromStreamAsync(audioFileStream);
        
        AudioContent audioContent = new(audioFileBinaryData, mimeType: null);
        #pragma warning restore SKEXP0001
        
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