using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Text;
using AngleSharp.Common;
using Microsoft.SemanticKernel.Process;
using YoutubeExplode.Videos;

namespace SemanticClip.Services;

#pragma warning disable SKEXP0080
// Video Processing Step 1: Prepare Video
public class PrepareVideoStep : KernelProcessStep
{
    public static class Functions
    {
        public const string PrepareVideo = nameof(PrepareVideo);
    }
    internal string _videoPath;
    // Video Processing Step 1: Confirm Video Existence
    [KernelFunction(Functions.PrepareVideo)]
    public async Task<string> PrepareVideoAsync(VideoProcessingRequest request, KernelProcessStepContext context)
    {
        
        /*void UpdateProgress(string status, int percentage, string currentOperation = "", string? error = null)
        {
            progressCallback?.Invoke(new VideoProcessingProgress
            {
                Status = status,
                Percentage = percentage,
                CurrentOperation = currentOperation,
                Error = error
            });
        }*/
        
        //UpdateProgress("Starting", 0, "Initializing");
        
        if (string.IsNullOrEmpty(request.YouTubeUrl) && string.IsNullOrEmpty(request.FileContent))
        {
            throw new ArgumentException("Either YouTube URL or file content must be provided");
        }

        
        
        if (!string.IsNullOrEmpty(request.YouTubeUrl))
        {
            //UpdateProgress("Downloading", 10, "Downloading YouTube video");
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(request.YouTubeUrl);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
            
            // Create a temporary file path
            var tempPath = Path.GetTempFileName();
            var fileExtension = streamInfo.Container.Name;
            var finalPath = Path.ChangeExtension(tempPath, fileExtension);
            File.Move(tempPath, finalPath);
            
            // Download the video to the temporary file
            await youtube.Videos.Streams.DownloadAsync(streamInfo, finalPath);
            _videoPath = finalPath;
        }
        else
        {
            //UpdateProgress("Processing", 10, "Processing uploaded file");
            // Create a temporary file path
            var tempPath = Path.GetTempFileName();
            var fileExtension = Path.GetExtension(request.FileName);
            var finalPath = Path.ChangeExtension(tempPath, fileExtension);
            File.Move(tempPath, finalPath);
            
            // Save the uploaded file
            var fileBytes = Convert.FromBase64String(request.FileContent!);
            await File.WriteAllBytesAsync(finalPath, fileBytes);
            _videoPath = finalPath;
        }
        
        await context.EmitEventAsync(new KernelProcessEvent{Id = "VideoPrepared", Data = _videoPath});
        return _videoPath;
    }
}

// Video Processing Step 2: Extract Audio and Transcribe
public class TranscribeVideoStep : KernelProcessStep
{
    private string _transcript = "";
    private ILogger logger = new LoggerFactory().CreateLogger<TranscribeVideoStep>();
    public static class Functions
    {
        public const string TranscribeVideo = nameof(TranscribeVideoStep);
    }
    [KernelFunction(Functions.TranscribeVideo)]
    public async Task<string> TranscribeVideoAsync(string videoPath, Kernel kernel, KernelProcessStepContext context)
    {
        /*void UpdateProgress(string status, int percentage, string currentOperation = "", string? error = null)
        {
            progressCallback?.Invoke(new VideoProcessingProgress
            {
                Status = status,
                Percentage = percentage,
                CurrentOperation = currentOperation,
                Error = error
            });
        }*/
        
        
        
        // Step 1: Extract audio from video using FFmpeg
        logger.LogInformation("Extracting audio from video: {VideoPath}", videoPath);
        //UpdateProgress("In Progress", 30, "Extracting audio from video");
        
        // Create a unique temporary file path for the extracted audio
        string outputAudioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
        
        try
        {
            logger.LogInformation("Starting audio extraction from: {VideoPath} to {AudioPath}", videoPath, outputAudioPath);
            
            // Execute the FFmpeg command to extract audio
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    // Extract audio with optimized settings for speech recognition
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
            
            logger.LogInformation("Running FFmpeg command");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // Use cancellation to prevent indefinite waiting
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            
            try {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) {
                logger.LogError("FFmpeg process timed out after 10 minutes");
                process.Kill(true);
                throw new Exception("Audio extraction timed out after 10 minutes");
            }
            
            if (process.ExitCode != 0)
            {
                string errorOutput = errorBuilder.ToString();
                logger.LogError("FFmpeg error: {Error}", errorOutput);
                throw new Exception($"Failed to extract audio: {errorOutput}");
            }
            
            if (!File.Exists(outputAudioPath))
            {
                throw new FileNotFoundException("Audio extraction did not produce the expected output file");
            }
            
            logger.LogInformation("Successfully extracted audio to: {AudioPath}", outputAudioPath);
            
            // Step 2: Transcribe the extracted audio with OpenAI Whisper
            logger.LogInformation("Transcribing audio: {AudioPath}", outputAudioPath);
            //UpdateProgress("In Progress", 50, "Transcribing audio");
            
            logger.LogInformation("Transcribing audio with Whisper via Semantic Kernel: {AudioPath}", outputAudioPath);
            
            // Use IAudioToTextService
            #pragma warning disable SKEXP0001
            var audioToTextService = kernel.GetRequiredService<IAudioToTextService>();
            
            // Read the audio file as a stream
            using var audioFileStream = new FileStream(outputAudioPath, FileMode.Open, FileAccess.Read);
            var audioFileBinaryData = await BinaryData.FromStreamAsync(audioFileStream!);
            
            AudioContent audioContent = new(audioFileBinaryData, mimeType: null);
            #pragma warning restore SKEXP0001
            
            // Transcribe the audio
            var result = await audioToTextService.GetTextContentAsync(audioContent);
            logger.LogInformation("Transcription completed successfully");
            
            // Store the transcript
            _transcript = result.Text;
            
            // Clean up the audio file
            try
            {
                File.Delete(outputAudioPath);
                logger.LogInformation("Deleted temporary audio file: {AudioPath}", outputAudioPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete temporary audio file: {AudioPath}", outputAudioPath);
            }
            
            // Instead of context.Set, emit the transcript as event payload
            await context.EmitEventAsync("TranscriptionComplete", _transcript);
            return _transcript;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during audio extraction or transcription");
            if (File.Exists(outputAudioPath))
            {
                try { File.Delete(outputAudioPath); } 
                catch { /* Ignore cleanup errors */ }
            }
            throw new Exception($"Audio processing failed: {ex.Message}", ex);
        }
    }
}

// Video Processing Step 3: Generate Chapters
public class GenerateChaptersStep : KernelProcessStep
{
    public static class Functions
    {
        public const string GenerateChaptersStep = nameof(GenerateChaptersStep);
    }
    internal List<Chapter> _chapters = new();
    [KernelFunction(Functions.GenerateChaptersStep)]
    public async Task<List<Chapter>> GenerateChaptersAsync(string transcript, ILogger logger, Action<VideoProcessingProgress> progressCallback, Kernel kernel, KernelProcessStepContext context)
    {
        void UpdateProgress(string status, int percentage, string currentOperation = "", string? error = null)
        {
            progressCallback?.Invoke(new VideoProcessingProgress
            {
                Status = status,
                Percentage = percentage,
                CurrentOperation = currentOperation,
                Error = error
            });
        }
        
        UpdateProgress("Generating", 60, "Generating chapters");
        
        // Using GPT-4o for content generation
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a helpful assistant that analyzes video transcripts and suggests logical chapter divisions with timestamps.");
        chat.AddUserMessage($"Please analyze this transcript and suggest chapters with timestamps:\n\n{transcript}");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        _chapters = ParseChaptersFromResponse(response.Content);
        
        await context.EmitEventAsync("ChaptersGenerated", _chapters);
        return _chapters;
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
}

// Video Processing Step 4: Generate Blog Post
public class GenerateBlogPostStep : KernelProcessStep
{   
    public static class Functions
    {
        public const string GenerateBlogPostStep = nameof(GenerateBlogPostStep);
    }
    internal string _blogPost = "";
    [KernelFunction(Functions.GenerateBlogPostStep)]
    public async Task<string> GenerateBlogPostAsync(string transcript, List<Chapter> chapters, ILogger logger, Action<VideoProcessingProgress> progressCallback, Kernel kernel, KernelProcessStepContext context)
    {
        void UpdateProgress(string status, int percentage, string currentOperation = "", string? error = null)
        {
            progressCallback?.Invoke(new VideoProcessingProgress
            {
                Status = status,
                Percentage = percentage,
                CurrentOperation = currentOperation,
                Error = error
            });
        }
        
        UpdateProgress("Creating", 80, "Creating blog post");
        
        // Using GPT-4o for content generation
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a professional content writer that creates engaging blog posts from video transcripts.");
        chat.AddUserMessage($"Please create a blog post based on this transcript and chapters:\n\nTranscript:\n{transcript}\n\nChapters:\n{string.Join("\n", chapters.Select(c => $"{c.Title} ({c.StartTime:hh\\:mm\\:ss} - {c.EndTime:hh\\:mm\\:ss})"))}");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        _blogPost = response.Content;
        
        await context.EmitEventAsync("BlogPostGenerated", _blogPost);
        return _blogPost;
    }
}

// Final Step: Complete Processing
public class CompletionStep : KernelProcessStep<VideoProcessingResponse>
{
    
    internal VideoProcessingResponse? _state;
    public override ValueTask ActivateAsync(KernelProcessStepState<VideoProcessingResponse> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }
    public static class Functions
    {
        public const string CompleteProcessing = nameof(CompleteProcessing);
    }
    [KernelFunction(Functions.CompleteProcessing)]
    public async Task<VideoProcessingResponse> CompleteProcessingAsync(string transcript, KernelProcessStepContext context)
    {
        /* Clean up video file
        if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
        {
            try
            {
                File.Delete(videoPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }*/
        // Create the response object
        this._state!.Status = "Completed";
        this._state!.Transcript = transcript;
        this._state!.Chapters = new List<Chapter>();
        this._state!.BlogPost = "";
        
    
        await context.EmitEventAsync("Completed", _state);
        
        return _state;
    }
}

public class VideoProcessingService : IVideoProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly Kernel _kernel; // Single kernel for both chat and audio-to-text
    private readonly ILogger<VideoProcessingService> _logger;
    private Action<VideoProcessingProgress>? _progressCallback;

    public VideoProcessingService(IConfiguration configuration, ILogger<VideoProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            _configuration["AzureOpenAI:ContentDeploymentName"]!,
            _configuration["AzureOpenAI:Endpoint"]!,
            _configuration["AzureOpenAI:ApiKey"]!);
        #pragma warning disable SKEXP0010
        builder.AddAzureOpenAIAudioToText(
            _configuration["AzureOpenAI:WhisperDeploymentName"]!,
            _configuration["AzureOpenAI:Endpoint"]!,
            _configuration["AzureOpenAI:ApiKey"]!);
        #pragma warning restore SKEXP0010
        _kernel = builder.Build();
    }

    public void SetProgressCallback(Action<VideoProcessingProgress>? callback)
    {
        _progressCallback = callback;
    }

    public void UpdateProgress(string status, int percentage, string currentOperation = "", string? error = null)
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
            // Create a new Semantic Kernel process
            #pragma warning disable SKEXP0080
            ProcessBuilder processBuilder = new("VideoProcessingWorkflow");
            
            // Add the processing steps
            var prepareVideoStep = processBuilder.AddStepFromType<PrepareVideoStep>();
            var transcribeVideoStep = processBuilder.AddStepFromType<TranscribeVideoStep>();
            //var generateChaptersStep = processBuilder.AddStepFromType<GenerateChaptersStep>();
            //var generateBlogPostStep = processBuilder.AddStepFromType<GenerateBlogPostStep>();
            var completionStep = processBuilder.AddStepFromType<CompletionStep>();
            
            
            // Orchestrate the workflow
            processBuilder
                .OnInputEvent("Start")
                .SendEventTo(new(prepareVideoStep, functionName: PrepareVideoStep.Functions.PrepareVideo,
                    parameterName: "request"));

            prepareVideoStep
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(transcribeVideoStep,
                    functionName: TranscribeVideoStep.Functions.TranscribeVideo,
                    parameterName: "videoPath"));
            
            transcribeVideoStep
                .OnFunctionResult()
                .SendEventTo(new ProcessFunctionTargetBuilder(completionStep,
                    functionName: CompletionStep.Functions.CompleteProcessing,
                    parameterName: "transcript"));
            
                
            /*prepareVideoStep
                .OnFunctionResult()
                .SendEventTo(new(transcribeVideoStep, functionName: TranscribeVideoStep.Functions.TranscribeVideoStep, parameterName: "videoPath"))
                .SendEventTo(new(transcribeVideoStep, functionName: TranscribeVideoStep.Functions.TranscribeVideoStep, parameterName: "logger"))
                .SendEventTo(new(transcribeVideoStep, functionName: TranscribeVideoStep.Functions.TranscribeVideoStep, parameterName: "progressCallback"))
                .SendEventTo(new(transcribeVideoStep, functionName: TranscribeVideoStep.Functions.TranscribeVideoStep, parameterName: "kernel"));
                
            transcribeVideoStep
                .OnFunctionResult()
                .SendEventTo(new(generateChaptersStep, functionName: GenerateChaptersStep.Functions.GenerateChaptersStep, parameterName: "transcript"))
                .SendEventTo(new(generateChaptersStep, functionName: GenerateChaptersStep.Functions.GenerateChaptersStep, parameterName: "logger"))
                .SendEventTo(new(generateChaptersStep, functionName: GenerateChaptersStep.Functions.GenerateChaptersStep, parameterName: "progressCallback"))
                .SendEventTo(new(generateChaptersStep, functionName: GenerateChaptersStep.Functions.GenerateChaptersStep, parameterName: "kernel"));
            
            generateChaptersStep
                .OnFunctionResult()
                .SendEventTo(new(generateBlogPostStep, functionName: GenerateBlogPostStep.Functions.GenerateBlogPostStep, parameterName: "chapters"))
                .SendEventTo(new(generateBlogPostStep, functionName: GenerateBlogPostStep.Functions.GenerateBlogPostStep, parameterName: "logger"))
                .SendEventTo(new(generateBlogPostStep, functionName: GenerateBlogPostStep.Functions.GenerateBlogPostStep, parameterName: "progressCallback"))
                .SendEventTo(new(generateBlogPostStep, functionName: GenerateBlogPostStep.Functions.GenerateBlogPostStep, parameterName: "kernel"));

            generateBlogPostStep
                .OnFunctionResult()
                .SendEventTo(new(completionStep, functionName: CompletionStep.Functions.CompleteProcessing,
                    parameterName: "blogPost"))
                .SendEventTo(new(completionStep, functionName: CompletionStep.Functions.CompleteProcessing,
                    parameterName: "transcript"))
                .SendEventTo(new(completionStep, functionName: CompletionStep.Functions.CompleteProcessing,
                    parameterName: "chapters"))
                .SendEventTo(new(completionStep, functionName: CompletionStep.Functions.CompleteProcessing,
                    parameterName: "videoPath"));
            */;
            
            // Build the process
            var process = processBuilder.Build();
            
            // Execute the workflow
            ///var result = await process.InvokeAsync("Start", new Dictionary<string, object> { { "request", request } });
            var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "Start", Data = request});
            var finalState = await initialResult.GetStateAsync();
            var finalCompletion = finalState.ToProcessStateMetadata();
            // Get the final state of the process
            //var completionStepState = finalState.Steps.Where(s => s.State.Name == "completionStep").FirstOrDefault()?.State as KernelProcessStepState<VideoProcessingResponse>;

            //if (completion == null)
            //{
            //    throw new Exception("Failed to retrieve completion step state");
            //}
            return finalCompletion.StepsState["CompletionStep"].State as VideoProcessingResponse ?? throw new Exception("Failed to retrieve completion step state");

#pragma warning restore SKEXP0080
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }

    public async Task<string> TranscribeVideoAsync(string videoPath)
    {
        try{
       #pragma warning disable SKEXP0080
        ProcessBuilder processBuilder = new("VideoProcessingWorkflow");
        var prepareVideoStep = processBuilder.AddStepFromType<PrepareVideoStep>();
        var transcribeVideoStep = processBuilder.AddStepFromType<TranscribeVideoStep>();

        prepareVideoStep
                .OnEvent("VideoPrepared")
                .SendEventTo(new(transcribeVideoStep, parameterName: "videoPath"))
                .SendEventTo(new(transcribeVideoStep, parameterName: "logger"))
                .SendEventTo(new(transcribeVideoStep, parameterName: "progressCallback"))
                .SendEventTo(new(transcribeVideoStep, parameterName: "kernel"));

        // Build the process
        var process = processBuilder.Build();
        
        var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "VideoPrepared", Data = null});
        var finalState = await initialResult.GetStateAsync();
        var finalCompletion = finalState.ToProcessStateMetadata();
        return (string)finalCompletion.State;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing video");
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }

    public async Task<List<Chapter>> GenerateChaptersAsync(string transcript)
    {
        try
        {
            #pragma warning disable SKEXP0080
            ProcessBuilder processBuilder = new("VideoProcessingWorkflow");
            var generateChaptersStep = processBuilder.AddStepFromType<GenerateChaptersStep>();

            generateChaptersStep
                .OnEvent("ChaptersGenerated")
                .SendEventTo(new(generateChaptersStep, parameterName: "transcript"))
                .SendEventTo(new(generateChaptersStep, parameterName: "logger"))
                .SendEventTo(new(generateChaptersStep, parameterName: "progressCallback"))
                .SendEventTo(new(generateChaptersStep, parameterName: "kernel"));

            // Build the process
            var process = processBuilder.Build();
            
            var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "ChaptersGenerated", Data = null});
            var finalState = await initialResult.GetStateAsync();
            var finalCompletion = finalState.ToProcessStateMetadata();
            return (List<Chapter>)finalCompletion.State;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chapters");
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }
    public async Task<string> GenerateBlogPostAsync(string transcript, List<Chapter> chapters)
    {
        try
        {
            #pragma warning disable SKEXP0080
            ProcessBuilder processBuilder = new("VideoProcessingWorkflow");
            var generateBlogPostStep = processBuilder.AddStepFromType<GenerateBlogPostStep>();

            generateBlogPostStep
                .OnEvent("BlogPostGenerated")
                .SendEventTo(new(generateBlogPostStep, parameterName: "transcript"))
                .SendEventTo(new(generateBlogPostStep, parameterName: "chapters"))
                .SendEventTo(new(generateBlogPostStep, parameterName: "logger"))
                .SendEventTo(new(generateBlogPostStep, parameterName: "progressCallback"))
                .SendEventTo(new(generateBlogPostStep, parameterName: "kernel"));

            // Build the process
            var process = processBuilder.Build();
            
            var initialResult = await process.StartAsync(_kernel, new KernelProcessEvent{Id = "BlogPostGenerated", Data = null});
            var finalState = await initialResult.GetStateAsync();
            var finalCompletion = finalState.ToProcessStateMetadata();
            return (string)finalCompletion.State;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating blog post");
            UpdateProgress("Error", 0, "Error occurred", ex.Message);
            throw;
        }
    }
    #pragma warning restore SKEXP0080
    
    
}