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
            _logger.LogInformation("Starting audio extraction from: {VideoPath} to {AudioPath}", videoPath, outputAudioPath);
            
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
            
            try {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) {
                _logger.LogError("FFmpeg process timed out after 10 minutes");
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
            
            _logger.LogInformation("Transcribing audio: {AudioPath}", outputAudioPath);
            
            #pragma warning disable SKEXP0001
            var audioToTextService = kernel.GetRequiredService<IAudioToTextService>();
            
            using var audioFileStream = new FileStream(outputAudioPath, FileMode.Open, FileAccess.Read);
            var audioFileBinaryData = await BinaryData.FromStreamAsync(audioFileStream!);
            
            AudioContent audioContent = new(audioFileBinaryData, mimeType: null);
            #pragma warning restore SKEXP0001
            var result = await audioToTextService.GetTextContentAsync(audioContent);
            _logger.LogInformation("Transcription completed successfully");
            
            _transcript = result.Text;
            
            try
            {
                File.Delete(outputAudioPath);
                _logger.LogInformation("Deleted temporary audio file: {AudioPath}", outputAudioPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary audio file: {AudioPath}", outputAudioPath);
            }
            
            await context.EmitEventAsync("TranscriptionComplete", _transcript);
            return _transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio extraction or transcription");
            if (File.Exists(outputAudioPath))
            {
                try { File.Delete(outputAudioPath); } 
                catch { /* Ignore cleanup errors */ }
            }
            throw new Exception($"Audio processing failed: {ex.Message}", ex);
        }
    }
} 