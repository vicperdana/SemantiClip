using Microsoft.AspNetCore.Mvc;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using SemanticClip.Infrastructure.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
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
    public async Task ProcessVideoWebSocket()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation("WebSocket connection established.");
            await HandleWebSocketConnection(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync("Expected a WebSocket request.");
        }
    }

    private async Task HandleWebSocketConnection(WebSocket webSocket)
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
                            await SendProgressUpdate(webSocket, progress);

                            // Process the video
                            var response = await _videoProcessingService.ProcessVideoAsync(request);

                            // Update progress
                            progress.Status = "Completed";
                            progress.Percentage = 100;
                            progress.Result = response;
                            await SendProgressUpdate(webSocket, progress);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize request: {Message}", message);
                            progress.Status = "Error";
                            progress.Error = "Failed to process request";
                            await SendProgressUpdate(webSocket, progress);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing video");
                            progress.Status = "Error";
                            progress.Error = ex.Message;
                            await SendProgressUpdate(webSocket, progress);
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
                    await SendProgressUpdate(webSocket, progress);
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

    private async Task SendProgressUpdate(WebSocket webSocket, VideoProcessingProgress progress)
    {
        var message = JsonSerializer.Serialize(progress);
        var bytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    [HttpPost("transcribe")]
    public async Task<ActionResult<string>> TranscribeVideo(IFormFile videoFile)
    {
        string? tempFilePath = null;
        try
        {
            // Save the uploaded file to a temporary path
            tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            // Call the service with the file path
            var transcript = await _videoProcessingService.TranscribeVideoAsync(tempFilePath);
            return Ok(transcript);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing video");
            return StatusCode(500, $"Error processing video: {ex.Message}");
        }
        finally
        {
            // Clean up the temporary file
            if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
            {
                try
                {
                    System.IO.File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary transcription file: {Path}", tempFilePath);
                }
            }
        }
    }
}