using System.Net.Http.Json;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;
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

   /* public async Task<VideoProcessingResponse> ProcessVideoAsync(VideoProcessingRequest request)
    {
        using var formData = new MultipartFormDataContent();

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
    }*/

    /*public async Task<string> TranscribeVideoAsync(Stream videoStream)
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
    }*/


    public async Task ProcessVideoAsync(IBrowserFile? videoFile, Func<VideoProcessingProgress, Task>? progressCallback = null)
    {
        ClientWebSocket? webSocket = null;
        try
        {
            var request = new VideoProcessingRequest();

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

    public async Task<BlogPublishingResponse> PublishBlogPostWithMcpAsync(string blogPost, string commitMessage, Func<BlogPublishingProgress, Task>? progressCallback = null)
    {
        try
        {
            var request = new BlogPostPublishRequest
            {
                BlogPost = blogPost,
                CommitMessage = commitMessage
            };
            
            // Initial progress update
            if (progressCallback != null)
            {
                await progressCallback(new BlogPublishingProgress
                {
                    Status = "Starting",
                    Percentage = 0,
                    CurrentOperation = "Preparing to publish blog post"
                });
            }
            
            // Make the API call
            var response = await _httpClient.PostAsJsonAsync("api/BlogPublishing/publish", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"Error publishing blog post: {response.StatusCode} - {errorContent}";
                
                if (progressCallback != null)
                {
                    await progressCallback(new BlogPublishingProgress
                    {
                        Status = "Failed",
                        Percentage = 100,
                        CurrentOperation = "Failed to publish blog post",
                        Error = errorMessage
                    });
                }
                
                return new BlogPublishingResponse 
                { 
                    Success = false, 
                    Message = errorMessage 
                };
            }
            
            // Parse the response
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BlogPublishingResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (result == null)
            {
                var errorMessage = "Failed to deserialize the response from the server.";
                
                if (progressCallback != null)
                {
                    await progressCallback(new BlogPublishingProgress
                    {
                        Status = "Failed",
                        Percentage = 100,
                        CurrentOperation = "Failed to publish blog post",
                        Error = errorMessage
                    });
                }
                
                return new BlogPublishingResponse 
                { 
                    Success = false, 
                    Message = errorMessage 
                };
            }
            
            // Success progress update
            if (progressCallback != null)
            {
                await progressCallback(new BlogPublishingProgress
                {
                    Status = result.Success ? "Completed" : "Failed",
                    Percentage = 100,
                    CurrentOperation = result.Success 
                        ? "Blog post published successfully" 
                        : result.Message,
                    Error = result.Success ? null : result.Message,
                    Result = result.Result
                });
            }
            
            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = $"An unexpected error occurred: {ex.Message}";
            
            if (progressCallback != null)
            {
                await progressCallback(new BlogPublishingProgress
                {
                    Status = "Failed",
                    Percentage = 100,
                    CurrentOperation = "Error publishing blog post",
                    Error = errorMessage
                });
            }
            
            _logger.LogError(ex, "Error publishing blog post");
            
            return new BlogPublishingResponse 
            { 
                Success = false, 
                Message = errorMessage 
            };
        }
    }
}
