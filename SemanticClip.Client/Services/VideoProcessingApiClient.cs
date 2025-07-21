using System.Net.Http.Json;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
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

            _logger.LogInformation("Starting video processing for file: {FileName}", request.FileName);

            // Start the video processing job
            var response = await _httpClient.PostAsJsonAsync("api/VideoProcessing/process", request);
            response.EnsureSuccessStatusCode();

            var jobResponse = await response.Content.ReadFromJsonAsync<JobStartResponse>();
            if (jobResponse?.JobId == null)
            {
                throw new InvalidOperationException("Failed to start video processing job - no job ID returned");
            }

            _logger.LogInformation("Video processing job started with ID: {JobId}", jobResponse.JobId);

            // Poll for progress updates
            await PollForProgressAsync(jobResponse.JobId, progressCallback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");
            
            if (progressCallback != null)
            {
                await progressCallback(new VideoProcessingProgress
                {
                    Status = "Failed",
                    Percentage = 0,
                    CurrentOperation = "Failed to process video",
                    Error = ex.Message
                });
            }
            
            throw;
        }
    }

    private async Task PollForProgressAsync(string jobId, Func<VideoProcessingProgress, Task>? progressCallback)
    {
        const int pollIntervalMs = 2000; // Poll every 2 seconds
        const int maxPollAttempts = 300; // 10 minutes max (300 * 2 seconds)
        
        var attempts = 0;
        
        while (attempts < maxPollAttempts)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/VideoProcessing/status/{jobId}");
                response.EnsureSuccessStatusCode();

                var jobStatus = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
                if (jobStatus?.Progress == null)
                {
                    _logger.LogWarning("Received null progress for job {JobId}", jobId);
                    await Task.Delay(pollIntervalMs);
                    attempts++;
                    continue;
                }

                _logger.LogDebug("Job {JobId} status: {Status} - {Operation} ({Percentage}%)", 
                    jobId, jobStatus.Status, jobStatus.Progress.CurrentOperation, jobStatus.Progress.Percentage);

                // Notify the UI about progress
                if (progressCallback != null)
                {
                    await progressCallback(jobStatus.Progress);
                }

                // Check if job is completed
                if (jobStatus.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Video processing job {JobId} completed successfully", jobId);
                    return;
                }
                
                if (jobStatus.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase) || 
                    jobStatus.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Video processing job {JobId} failed with status: {Status}", jobId, jobStatus.Status);
                    return;
                }

                await Task.Delay(pollIntervalMs);
                attempts++;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to poll job status for {JobId}, attempt {Attempt}/{MaxAttempts}", 
                    jobId, attempts + 1, maxPollAttempts);
                
                await Task.Delay(pollIntervalMs);
                attempts++;
            }
        }

        _logger.LogError("Polling for job {JobId} timed out after {Attempts} attempts", jobId, attempts);
        
        if (progressCallback != null)
        {
            await progressCallback(new VideoProcessingProgress
            {
                Status = "Failed",
                Percentage = 0,
                CurrentOperation = "Polling timeout - job status unknown",
                Error = "Failed to get final job status within the expected time"
            });
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
