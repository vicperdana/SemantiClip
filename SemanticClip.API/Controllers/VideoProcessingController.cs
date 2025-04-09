using Microsoft.AspNetCore.Mvc;
using SemanticClip.Core.Models;
using SemanticClip.Core.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace SemanticClip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoProcessingController : ControllerBase
{
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly ILogger<VideoProcessingController> _logger;
    private readonly IConfiguration _configuration;
    private readonly long _maxRequestBodySize;

    public VideoProcessingController(
        IVideoProcessingService videoProcessingService,
        ILogger<VideoProcessingController> logger,
        IConfiguration configuration)
    {
        _videoProcessingService = videoProcessingService;
        _logger = logger;
        _configuration = configuration;
        _maxRequestBodySize = _configuration.GetValue<long>("FileUpload:MaxRequestBodySizeInBytes");
    }

    [HttpPost("process")]
    [RequestSizeLimit(bytes: long.MaxValue)] // Will be overridden by the middleware
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)] // Will be overridden by the middleware
    public async Task<ActionResult<VideoProcessingResponse>> ProcessVideo([FromForm] VideoProcessingRequest request)
    {
        try
        {
            // Get the request size limit from configuration
            var bodySizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodySizeFeature != null)
            {
                bodySizeFeature.MaxRequestBodySize = _maxRequestBodySize;
            }

            var formOptions = new FormOptions
            {
                MultipartBodyLengthLimit = _maxRequestBodySize
            };

            _logger.LogInformation("Processing video request: {Request}", request);
            var response = await _videoProcessingService.ProcessVideoAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video request");
            return StatusCode(500, new VideoProcessingResponse
            {
                Status = "Failed",
                ErrorMessage = ex.Message
            });
        }
    }

    [HttpPost("transcribe")]
    [RequestSizeLimit(bytes: long.MaxValue)] // Will be overridden by the middleware
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)] // Will be overridden by the middleware
    public async Task<ActionResult<string>> TranscribeVideo(IFormFile videoFile)
    {
        try
        {
            // Get the request size limit from configuration
            var bodySizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodySizeFeature != null)
            {
                bodySizeFeature.MaxRequestBodySize = _maxRequestBodySize;
            }

            var formOptions = new FormOptions
            {
                MultipartBodyLengthLimit = _maxRequestBodySize
            };

            using var stream = videoFile.OpenReadStream();
            var transcript = await _videoProcessingService.TranscribeVideoAsync(stream);
            return Ok(transcript);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing video");
            return StatusCode(500, $"Error processing video: {ex.Message}");
        }
    }
}