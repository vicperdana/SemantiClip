using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Text;

namespace SemanticClip.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly Kernel _kernel;
    private readonly SpeechConfig _speechConfig;
    private readonly ILogger<VideoProcessingService> _logger;
    private Action<VideoProcessingProgress>? _progressCallback;

    public VideoProcessingService(IConfiguration configuration, ILogger<VideoProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Initialize Semantic Kernel
        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            _configuration["AzureOpenAI:DeploymentName"]!,
            _configuration["AzureOpenAI:Endpoint"]!,
            _configuration["AzureOpenAI:ApiKey"]!);
        _kernel = builder.Build();

        // Initialize Speech Config
        _speechConfig = SpeechConfig.FromSubscription(
            _configuration["AzureSpeech:Key"]!,
            _configuration["AzureSpeech:Region"]!);
        _speechConfig.SetProperty(PropertyId.SpeechServiceResponse_JsonResult, "true");
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
        var stopRecognition = new TaskCompletionSource<int>();
        var transcriptBuilder = new StringBuilder();
        var totalBytes = new FileInfo(videoPath).Length;
        var processedBytes = 0L;

        using var videoStream = File.OpenRead(videoPath);
        var audioConfig = AudioConfig.FromStreamInput(new PullAudioInputStream(new AudioStreamReader(videoStream, (bytesProcessed) =>
        {
            processedBytes += bytesProcessed;
            var progress = (int)((processedBytes * 100) / totalBytes);
            UpdateProgress("In Progress", progress, "Transcribing video");
        })));
        using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                transcriptBuilder.AppendLine(e.Result.Text);
                _logger.LogInformation("Recognized: {Text}", e.Result.Text);
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                _logger.LogWarning("NOMATCH: Speech could not be recognized.");
            }
        };

        recognizer.Canceled += (s, e) =>
        {
            _logger.LogWarning($"CANCELED: Reason={e.Reason}");
            if (e.Reason == CancellationReason.Error)
            {
                _logger.LogError($"CANCELED: ErrorCode={e.ErrorCode}");
                _logger.LogError($"CANCELED: ErrorDetails={e.ErrorDetails}");
            }
            stopRecognition.TrySetResult(0);
        };

        recognizer.SessionStopped += (s, e) =>
        {
            _logger.LogInformation("Session stopped event.");
            stopRecognition.TrySetResult(0);
        };

        await recognizer.StartContinuousRecognitionAsync();
        await stopRecognition.Task;
        await recognizer.StopContinuousRecognitionAsync();

        return transcriptBuilder.ToString();
    }

    private class AudioStreamReader : PullAudioInputStreamCallback
    {
        private readonly Stream _stream;
        private readonly Action<long> _progressCallback;

        public AudioStreamReader(Stream stream, Action<long> progressCallback)
        {
            _stream = stream;
            _progressCallback = progressCallback;
        }

        public override int Read(byte[] dataBuffer, uint size)
        {
            try
            {
                var bytesRead = _stream.Read(dataBuffer, 0, (int)size);
                _progressCallback(bytesRead);
                return bytesRead;
            }
            catch (Exception ex)
            {
                // Log the error using the parent service's logger
                throw new Exception("Error reading from audio stream", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public async Task<List<Chapter>> GenerateChaptersAsync(string transcript)
    {
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
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
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a professional content writer that creates engaging blog posts from video transcripts.");
        chat.AddUserMessage($"Please create a blog post based on this transcript and chapters:\n\nTranscript:\n{transcript}\n\nChapters:\n{string.Join("\n", chapters.Select(c => $"{c.Title} ({c.StartTime:hh\\:mm\\:ss} - {c.EndTime:hh\\:mm\\:ss})"))}");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        return response.Content;
    }
}