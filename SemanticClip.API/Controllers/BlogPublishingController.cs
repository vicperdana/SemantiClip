using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticClip.Core.Models;
using SemanticClip.Services;

namespace SemanticClip.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogPublishingController : ControllerBase
    {
        private readonly ILogger<BlogPublishingController> _logger;
        private readonly BlogPublishingService _blogPublishingService;
        
        public BlogPublishingController(
            ILogger<BlogPublishingController> logger,
            BlogPublishingService blogPublishingService)
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
                
                _logger.LogInformation("Blog post successfully published");
                
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing blog post: {Error}", ex.Message);
                return StatusCode(500, $"Error publishing blog post: {ex.Message}");
            }
        }
        

    }
}
