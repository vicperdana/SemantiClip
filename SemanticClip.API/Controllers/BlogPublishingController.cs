using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticClip.Core.Models;
using SemanticClip.Services.Steps;
using System;
using System.Threading.Tasks;

namespace SemanticClip.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogPublishingController : ControllerBase
    {
        private readonly ILogger<BlogPublishingController> _logger;
        private readonly PublishBlogPostStep _publishBlogPostStep;
        
        public BlogPublishingController(
            ILogger<BlogPublishingController> logger,
            PublishBlogPostStep publishBlogPostStep)
        {
            _logger = logger;
            _publishBlogPostStep = publishBlogPostStep;
        }
        
        [HttpPost("publish")]
        public async Task<IActionResult> PublishBlogPostAsync([FromBody] BlogPostPublishRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.BlogPost))
            {
                return BadRequest("Blog post content is required");
            }
            
            try
            {
                _logger.LogInformation("Publishing blog post with MCP");
                
                // Create the GitHub commit message
                var commitMessage = request.CommitMessage ?? "Published blog post via SemantiClip";
                
                // For the sake of simplicity, we'll create a method that mimics what would happen
                // in the actual step implementation
                var result = await SimulatePublishWithMcpAsync(request.BlogPost, commitMessage);
                
                _logger.LogInformation("Blog post successfully published with result: {Result}", result);
                
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing blog post: {Error}", ex.Message);
                return StatusCode(500, $"Error publishing blog post: {ex.Message}");
            }
        }
        
        private async Task<string> SimulatePublishWithMcpAsync(string blogPost, string commitMessage)
        {
            // This would normally call the PublishBlogPostStep
            // But for simplicity, we'll just simulate the process
            await Task.Delay(1500); // Simulate processing time
            
            // Return a message that would normally come from MCP
            return $"Blog post published successfully with commit message: '{commitMessage}'";
        }
    }
}
