using System.Net.Http.Json;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SemanticClip.Client.Services;

public class VideoProcessingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly long _maxRequestBodySize;
    private readonly ILogger<VideoProcessingApiClient> _logger;

    public VideoProcessingApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<VideoProcessingApiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // Read max body size from config, fallback to 3MB
        var configuredValue = _configuration.GetValue<long?>("MaxRequestBodySize");
        _maxRequestBodySize = configuredValue ?? 3_000_000;

        Console.WriteLine($"Configured max file size: {_maxRequestBodySize} bytes");
    }

    public async Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request)
    {
        using var formData = new MultipartFormDataContent();

        if (!string.IsNullOrEmpty(request.YouTubeUrl))
        {
            formData.Add(new StringContent(request.YouTubeUrl), "youtubeUrl");
        }

        if (!string.IsNullOrEmpty(request.FileContent) && !string.IsNullOrEmpty(request.FileName))
        {
            var fileBytes = Convert.FromBase64String(request.FileContent);
            var stream = new MemoryStream(fileBytes);
            formData.Add(new StreamContent(stream), "videoFile", request.FileName);
        }

        var response = await _httpClient.PostAsync("api/VideoProcessing/process", formData);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VideoProcessingResponse>()
            ?? throw new Exception("Failed to deserialize response");
    }

    public async Task<string> TranscribeVideoAsync(Stream videoStream)
    {
        Console.WriteLine($"Current max size: {_maxRequestBodySize}, File size: {videoStream.Length}");

        if (videoStream.Length > _maxRequestBodySize)
        {
            throw new InvalidOperationException($"File size ({videoStream.Length} bytes) exceeds the maximum allowed size of {_maxRequestBodySize} bytes");
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(videoStream), "videoFile", "video.mp4");

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

     public async Task ProcessVideoAsync(string youtubeUrl, IBrowserFile? videoFile, Func<VideoProcessingProgress, Task>? progressCallback = null)
    {
        ClientWebSocket? webSocket = null;
        try
        {
            var request = new VideoProcessingRequest
            {
                YouTubeUrl = youtubeUrl
            };

            if (videoFile != null)
            {
                using var stream = videoFile.OpenReadStream(maxAllowedSize: 30_000_000); // 30MB max
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                request.FileName = videoFile.Name;
                request.FileContent = Convert.ToBase64String(memoryStream.ToArray());
            }

            var baseAddress = _httpClient.BaseAddress ?? new Uri("http://localhost:5290");
            var wsUri = new UriBuilder(baseAddress)
            {
                Scheme = baseAddress.Scheme == "https" ? "wss" : "ws",
                Path = "api/VideoProcessing/process"
            }.Uri;

            webSocket = new ClientWebSocket();
            
            // Add a connection timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            try
            {
                await webSocket.ConnectAsync(wsUri, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("WebSocket connection timed out");
            }

            if (webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException($"WebSocket connection failed. State: {webSocket.State}");
            }

            var requestJson = JsonSerializer.Serialize(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);
            
            // Send the request in chunks if it's too large
            const int chunkSize = 8192; // 8KB chunks
            for (int offset = 0; offset < requestBytes.Length; offset += chunkSize)
            {
                var remainingBytes = requestBytes.Length - offset;
                var currentChunkSize = Math.Min(chunkSize, remainingBytes);
                var isLastChunk = offset + currentChunkSize >= requestBytes.Length;
                
                await webSocket.SendAsync(
                    new ArraySegment<byte>(requestBytes, offset, currentChunkSize),
                    WebSocketMessageType.Text,
                    isLastChunk,
                    CancellationToken.None);
            }

            // Use a larger buffer for receiving messages
            var buffer = new byte[32768]; // 32KB buffer
            var messageBuilder = new StringBuilder();
            
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    
                    if (result.EndOfMessage)
                    {
                        var message = messageBuilder.ToString();
                        messageBuilder.Clear();
                        
                        try
                        {
                            var progressUpdate = JsonSerializer.Deserialize<VideoProcessingProgress>(message);
                            if (progressUpdate != null && progressCallback != null)
                            {
                                await progressCallback(progressUpdate);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize progress update: {Message}", message);
                            throw;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");
            throw;
        }
        finally
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing WebSocket connection");
                }
            }
            webSocket?.Dispose();
        }
    }
}
