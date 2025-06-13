using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;

namespace SemanticClip.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogPublishingController : ControllerBase
    {
        private readonly ILogger<BlogPublishingController> _logger;
        private readonly IBlogPublishingService _blogPublishingService;
        
        public BlogPublishingController(
            ILogger<BlogPublishingController> logger,
            IBlogPublishingService blogPublishingService)
        {
            _logger = logger;
            _blogPublishingService = blogPublishingService;
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
                _logger.LogInformation("Publishing blog post");
                
                var result = await _blogPublishingService.PublishBlogPostAsync(request);
                
                if (result.Success)
                {
                    _logger.LogInformation("Blog post successfully published");
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Failed to publish blog post: {Message}", result.Message);
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error publishing blog post: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new BlogPublishingResponse 
                { 
                    Success = false, 
                    Message = errorMessage 
                });
            }
        }
        

    }
}
