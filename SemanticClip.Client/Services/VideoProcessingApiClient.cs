using System.Net.Http.Json;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace SemanticClip.Client.Services;

public class VideoProcessingApiClient : IVideoProcessingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly long _maxRequestBodySize;

    public VideoProcessingApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        // Try to get the configured value, fall back to 3MB if not found
        var configuredValue = _configuration.GetValue<long?>("FileUpload:MaxRequestBodySizeInBytes");
        _maxRequestBodySize = configuredValue ?? 3000000;
        
        Console.WriteLine($"Configured max file size: {_maxRequestBodySize} bytes");
    }

    public async Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request)
    {
        // Create a multipart form data content
        using var formData = new MultipartFormDataContent();
        
        // Add YouTube URL if provided
        if (!string.IsNullOrEmpty(request.YouTubeUrl))
        {
            formData.Add(new StringContent(request.YouTubeUrl), "youtubeUrl");
        }
        
        // Add video file if provided
        if (request.VideoFile != null)
        {
            using var streamContent = new StreamContent(request.VideoFile.OpenReadStream());
            formData.Add(streamContent, "videoFile", request.VideoFile.Name);
        }
        
        // Send the request
        var response = await _httpClient.PostAsync("api/VideoProcessing/process", formData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VideoProcessingResponse>() 
            ?? throw new Exception("Failed to deserialize response");
    }

    public async Task<string> TranscribeVideoAsync(Stream videoStream)
    {
        // Log the actual size limits for diagnostic purposes
        Console.WriteLine($"Current max size: {_maxRequestBodySize}, File size: {videoStream.Length}");
        
        if (videoStream.Length > _maxRequestBodySize)
        {
            throw new InvalidOperationException($"File size ({videoStream.Length} bytes) exceeds the maximum allowed size of {_maxRequestBodySize} bytes ({_maxRequestBodySize / 1024 / 1024}MB)");
        }

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(videoStream);
        content.Add(streamContent, "videoFile", "video.mp4");

        var response = await _httpClient.PostAsync("api/VideoProcessing/transcribe", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<List<Chapter>> GenerateChaptersAsync(string transcript)
    {
        var response = await _httpClient.PostAsJsonAsync("api/VideoProcessing/generate-chapters", transcript);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Chapter>>() 
            ?? throw new Exception("Failed to deserialize chapters");
    }

    public async Task<string> GenerateBlogPostAsync(string transcript, List<Chapter> chapters)
    {
        var request = new { Transcript = transcript, Chapters = chapters };
        var response = await _httpClient.PostAsJsonAsync("api/VideoProcessing/generate-blog", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}