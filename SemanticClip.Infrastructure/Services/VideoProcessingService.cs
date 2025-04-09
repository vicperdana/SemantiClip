using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace SemanticClip.Infrastructure.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly Kernel _kernel;
    private readonly SpeechConfig _speechConfig;

    public VideoProcessingService(IConfiguration configuration)
    {
        _configuration = configuration;
        
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
    }

    public async Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request)
    {
        try
        {
            var response = new VideoProcessingResponse();
            
            // Download video if YouTube URL is provided
            Stream videoStream;
            if (!string.IsNullOrEmpty(request.YouTubeUrl))
            {
                videoStream = await DownloadYouTubeVideoAsync(request.YouTubeUrl);
            }
            else if (request.VideoFile != null)
            {
                videoStream = request.VideoFile.OpenReadStream();
            }
            else
            {
                throw new ArgumentException("Either YouTube URL or video file must be provided");
            }

            // Transcribe video
            //response.Transcript = await TranscribeVideoAsync(videoStream);

            // Generate chapters
            //response.Chapters = await GenerateChaptersAsync(response.Transcript);

            // Generate blog post
            //response.BlogPost = await GenerateBlogPostAsync(response.Transcript, response.Chapters);

            response.Status = "Completed";
            return response;
        }
        catch (Exception ex)
        {
            return new VideoProcessingResponse
            {
                Status = "Failed",
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<Stream> DownloadYouTubeVideoAsync(string youtubeUrl)
    {
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(youtubeUrl);
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
        return await youtube.Videos.Streams.GetAsync(streamInfo);
    }

    public async Task<string> TranscribeVideoAsync(Stream videoStream)
    {
        var audioConfig = AudioConfig.FromStreamInput(new PullAudioInputStream(new AudioStreamReader(videoStream)));
        using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();
        return result.Text;
    }

    private class AudioStreamReader : PullAudioInputStreamCallback
    {
        private readonly Stream _stream;

        public AudioStreamReader(Stream stream)
        {
            _stream = stream;
        }

        public override int Read(byte[] dataBuffer, uint size)
        {
            return _stream.Read(dataBuffer, 0, (int)size);
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
        // This is a simplified implementation. In a real application,
        // you would want to use a more robust parsing strategy
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
        chat.AddUserMessage($"Please create a blog post based on this transcript and chapters:\n\nTranscript:\n{transcript}\n\nChapters:\n{string.Join("\n", chapters.Select(c => $"{c.Title} ({c.StartTime}-{c.EndTime})"))}");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        return response.Content;
    }
} 