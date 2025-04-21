using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Process;
using SemanticClip.Core.Models;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace SemanticClip.Services.Steps;

#pragma warning disable SKEXP0080
public class PrepareVideoStep : KernelProcessStep
{
    public static class Functions
    {
        public const string PrepareVideo = nameof(PrepareVideo);
    }
    internal string? _videoPath;

    [KernelFunction(Functions.PrepareVideo)]
    public async Task<string> PrepareVideoAsync(VideoProcessingRequest request, KernelProcessStepContext context)
    {
        if (string.IsNullOrEmpty(request.YouTubeUrl) && string.IsNullOrEmpty(request.FileContent))
        {
            throw new ArgumentException("Either YouTube URL or file content must be provided");
        }

        if (!string.IsNullOrEmpty(request.YouTubeUrl))
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(request.YouTubeUrl);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
            
            var tempPath = Path.GetTempFileName();
            var fileExtension = streamInfo.Container.Name;
            var finalPath = Path.ChangeExtension(tempPath, fileExtension);
            File.Move(tempPath, finalPath);
            
            await youtube.Videos.Streams.DownloadAsync(streamInfo, finalPath);
            _videoPath = finalPath;
        }
        else
        {
            var tempPath = Path.GetTempFileName();
            var fileExtension = Path.GetExtension(request.FileName);
            var finalPath = Path.ChangeExtension(tempPath, fileExtension);
            File.Move(tempPath, finalPath);
            
            var fileBytes = Convert.FromBase64String(request.FileContent!);
            await File.WriteAllBytesAsync(finalPath, fileBytes);
            _videoPath = finalPath;
        }
        
        await context.EmitEventAsync(new KernelProcessEvent{Id = "VideoPrepared", Data = _videoPath});
        return _videoPath;
    }
} 