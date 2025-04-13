using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Text;

namespace SemanticClip.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly Kernel _contentKernel;  // For content generation (GPT-4o)
    private readonly Kernel _whisperKernel;  // For audio transcription (Whisper)
    private readonly ILogger<VideoProcessingService> _logger;
    private Action<VideoProcessingProgress>? _progressCallback;

    public VideoProcessingService(IConfiguration configuration, ILogger<VideoProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Initialize Content Kernel (GPT-4o)
        var contentBuilder = Kernel.CreateBuilder();
        contentBuilder.AddAzureOpenAIChatCompletion(
            _configuration["AzureOpenAI:ContentDeploymentName"]!,
            _configuration["AzureOpenAI:Endpoint"]!,
            _configuration["AzureOpenAI:ApiKey"]!);
        _contentKernel = contentBuilder.Build();
        
        // Initialize Whisper Kernel
        var whisperBuilder = Kernel.CreateBuilder();
        whisperBuilder.AddAzureOpenAIChatCompletion(
            _configuration["AzureOpenAI:WhisperDeploymentName"]!,
            _configuration["AzureOpenAI:Endpoint"]!,
            _configuration["AzureOpenAI:ApiKey"]!);
        _whisperKernel = whisperBuilder.Build();
    }

    public void SetProgressCallback(Action<VideoProcessingProgress>? callback)
    {
        _progressCallback = callback;
    }

    private void UpdateProgress(string status, int percentage, string currentOperation = "", string? error = null)
    {
        if (_progressCallback != null)
        {
            var progress = new VideoProcessingProgress
            {
                Status = status,
                Percentage = percentage,
                CurrentOperation = currentOperation,
                Error = error
            };
            _progressCallback(progress);
        }
    }

    public async Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request)
    {
        try
        {
            UpdateProgress("Starting", 0, "Initializing");
            
            if (string.IsNullOrEmpty(request.YouTubeUrl) && string.IsNullOrEmpty(request.FileContent))
            {
                throw new ArgumentException("Either YouTube URL or file content must be provided");
            }

            string? videoPath = null;
            try
            {
                if (!string.IsNullOrEmpty(request.YouTubeUrl))
                {
                    UpdateProgress("Downloading", 10, "Downloading YouTube video");
                    videoPath = await DownloadYouTubeVideoAsync(request.YouTubeUrl);
                }
                else if (!string.IsNullOrEmpty(request.FileContent))
                {
                    UpdateProgress("Processing", 10, "Processing uploaded file");
                    videoPath = await SaveUploadedFileAsync(request.FileName, request.FileContent);
                }

                if (string.IsNullOrEmpty(videoPath))
                {
                    throw new InvalidOperationException("Failed to obtain video file");
                }

                UpdateProgress("Transcribing", 30, "Transcribing video");
                var transcript = await TranscribeVideoAsync(videoPath);

                UpdateProgress("Generating", 60, "Generating chapters");
                var chapters = await GenerateChaptersAsync(transcript);

                UpdateProgress("Creating", 80, "Creating blog post");
                var blogPost = await GenerateBlogPostAsync(transcript, chapters);

                UpdateProgress("Completed", 100, "Processing complete");

                return new VideoProcessingResponse
                {
                    Status = "Completed",
                    Transcript = transcript,
                    Chapters = chapters,
                    BlogPost = blogPost
                };
            }
            finally
            {
                if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
                {
                    try
                    {
                        File.Delete(videoPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary video file: {Path}", videoPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }

    private async Task<string> DownloadYouTubeVideoAsync(string youtubeUrl)
    {
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(youtubeUrl);
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
        
        // Create a temporary file path
        var tempPath = Path.GetTempFileName();
        var fileExtension = streamInfo.Container.Name;
        var finalPath = Path.ChangeExtension(tempPath, fileExtension);
        File.Move(tempPath, finalPath);
        
        // Download the video to the temporary file
        await youtube.Videos.Streams.DownloadAsync(streamInfo, finalPath);
        
        return finalPath;
    }

    private async Task<string> SaveUploadedFileAsync(string fileName, string fileContent)
    {
        // Create a temporary file path
        var tempPath = Path.GetTempFileName();
        var fileExtension = Path.GetExtension(fileName);
        var finalPath = Path.ChangeExtension(tempPath, fileExtension);
        File.Move(tempPath, finalPath);
        
        // Save the uploaded file
        var fileBytes = Convert.FromBase64String(fileContent);
        await File.WriteAllBytesAsync(finalPath, fileBytes);
        
        return finalPath;
    }

    public async Task<string> TranscribeVideoAsync(string videoPath)
    {
        try
        {
            // Step 1: Extract audio from video using FFmpeg
            _logger.LogInformation("Extracting audio from video: {VideoPath}", videoPath);
            UpdateProgress("In Progress", 30, "Extracting audio from video");
            
            string audioPath = await ExtractAudioFromVideoAsync(videoPath);
            
            try
            {
                // Step 2: Transcribe the extracted audio with OpenAI Whisper
                _logger.LogInformation("Transcribing audio: {AudioPath}", audioPath);
                UpdateProgress("In Progress", 50, "Transcribing audio");
                
                return await TranscribeWithWhisperAsync(audioPath);
            }
            finally
            {
                // Clean up the temporary audio file
                if (File.Exists(audioPath))
                {
                    try
                    {
                        File.Delete(audioPath);
                        _logger.LogInformation("Deleted temporary audio file: {AudioPath}", audioPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary audio file: {AudioPath}", audioPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing video: {VideoPath}", videoPath);
            throw new Exception($"Video transcription failed: {ex.Message}", ex);
        }
    }
    
    private async Task<string> ExtractAudioFromVideoAsync(string videoFilePath)
    {
        // Create a unique temporary file path for the extracted audio
        string outputAudioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
        
        try
        {
            _logger.LogInformation("Starting audio extraction from: {VideoPath} to {AudioPath}", videoFilePath, outputAudioPath);
            
            // Execute the FFmpeg command to extract audio
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    // Extract audio with optimized settings for speech recognition
                    Arguments = $"-i \"{videoFilePath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputAudioPath}\" -y",
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
            
            // Use cancellation to prevent indefinite waiting
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
            return outputAudioPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio extraction");
            if (File.Exists(outputAudioPath))
            {
                try { File.Delete(outputAudioPath); } 
                catch { /* Ignore cleanup errors */ }
            }
            throw new Exception($"Audio extraction failed: {ex.Message}", ex);
        }
    }
    
    private async Task<string> TranscribeWithWhisperAsync(string audioFilePath)
    {
        try
        {
            // Using Whisper model for transcription
            _logger.LogInformation("Transcribing audio with Whisper via Azure OpenAI: {AudioPath}", audioFilePath);
            
            var chatCompletion = _whisperKernel.GetRequiredService<IChatCompletionService>();
            var chat = new ChatHistory();
            
            // Read the audio file as bytes
            byte[] audioBytes = await File.ReadAllBytesAsync(audioFilePath);
            string audioBase64 = Convert.ToBase64String(audioBytes);
            
            // Instruct the model to transcribe the audio
            chat.AddSystemMessage("You are a helpful assistant that transcribes audio files. Please transcribe the audio content accurately.");
            chat.AddUserMessage($"Transcribe the following audio file (base64 encoded WAV format): {audioBase64.Substring(0, Math.Min(50, audioBase64.Length))}...[base64 audio content]");
            
            // Get the transcription
            var response = await chatCompletion.GetChatMessageContentAsync(chat);
            _logger.LogInformation("Transcription completed successfully");
            
            return response.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing with Whisper API");
            throw new Exception($"Whisper transcription failed: {ex.Message}", ex);
        }
    }

    public async Task<List<Chapter>> GenerateChaptersAsync(string transcript)
    {
        // Using GPT-4o for content generation
        var chatCompletion = _contentKernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a helpful assistant that analyzes video transcripts and suggests logical chapter divisions with timestamps.");
        chat.AddUserMessage($"Please analyze this transcript and suggest chapters with timestamps:\n\n{transcript}");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        return ParseChaptersFromResponse(response.Content);
    }

    private List<Chapter> ParseChaptersFromResponse(string response)
    {
        var chapters = new List<Chapter>();
        var lines = response.Split('\n');

        foreach (var line in lines)
        {
            if (line.Contains(":"))
            {
                var parts = line.Split(':');
                if (parts.Length >= 2)
                {
                    var timePart = parts[0].Trim();
                    var titlePart = parts[1].Trim();

                    if (TimeSpan.TryParse(timePart, out var time))
                    {
                        chapters.Add(new Chapter
                        {
                            Title = titlePart,
                            StartTime = time,
                            EndTime = time.Add(TimeSpan.FromMinutes(5)) // Default 5-minute chapter length
                        });
                    }
                }
            }
        }

        return chapters;
    }

    public async Task<string> GenerateBlogPostAsync(string transcript, List<Chapter> chapters)
    {
        // Using GPT-4o for content generation
        var chatCompletion = _contentKernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a professional content writer that creates engaging blog posts from video transcripts.");
        chat.AddUserMessage($"Please create a blog post based on this transcript and chapters:\n\nTranscript:\n{transcript}\n\nChapters:\n{string.Join("\n", chapters.Select(c => $"{c.Title} ({c.StartTime:hh\\:mm\\:ss} - {c.EndTime:hh\\:mm\\:ss})"))}");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        return response.Content;
    }
}