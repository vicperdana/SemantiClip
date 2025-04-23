using Microsoft.AspNetCore.Mvc;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SemanticClip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoProcessingController : ControllerBase
{
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly ILogger<VideoProcessingController> _logger;

    public VideoProcessingController(
        IVideoProcessingService videoProcessingService,
        ILogger<VideoProcessingController> logger)
    {
        _videoProcessingService = videoProcessingService;
        _logger = logger;
    }

    [HttpGet("process")]
    public async Task ProcessVideoWebSocketAsync()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation("WebSocket connection established.");
            await HandleWebSocketConnectionAsync(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync("Expected a WebSocket request.");
        }
    }

    private async Task HandleWebSocketConnectionAsync(WebSocket webSocket)
    {
        var buffer = new byte[32768]; // 32KB buffer
        var messageBuilder = new StringBuilder();
        var request = new VideoProcessingRequest();
        var progress = new VideoProcessingProgress();

        try
        {
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
                            request = JsonSerializer.Deserialize<VideoProcessingRequest>(message)
                                ?? throw new JsonException("Failed to deserialize request");

                            // Update progress
                            progress.Status = "Processing video...";
                            progress.Percentage = 10;
                            await SendProgressUpdateAsync(webSocket, progress);

                            // Process the video
                            var response = await _videoProcessingService.ProcessVideoAsync(request);

                            // Update progress
                            progress.Status = "Completed";
                            progress.Percentage = 100;
                            progress.Result = response;
                            await SendProgressUpdateAsync(webSocket, progress);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize request: {Message}", message);
                            progress.Status = "Error";
                            progress.Error = "Failed to process request";
                            await SendProgressUpdateAsync(webSocket, progress);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing video");
                            progress.Status = "Error";
                            progress.Error = ex.Message;
                            await SendProgressUpdateAsync(webSocket, progress);
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during WebSocket communication");
            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    progress.Status = "Error";
                    progress.Error = ex.Message;
                    await SendProgressUpdateAsync(webSocket, progress);
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Error occurred", CancellationToken.None);
                }
                catch (Exception closeEx)
                {
                    _logger.LogError(closeEx, "Error closing WebSocket connection");
                }
            }
        }
        finally
        {
            if (webSocket.State == WebSocketState.Open)
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
            webSocket.Dispose();
        }
    }

    private async Task SendProgressUpdateAsync(WebSocket webSocket, VideoProcessingProgress progress)
    {
        var message = JsonSerializer.Serialize(progress);
        var bytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}