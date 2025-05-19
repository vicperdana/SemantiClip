using SemanticClip.Core.Models;

namespace SemanticClip.Core.Interfaces;

/// <summary>
/// Defines the contract for a service that handles blog post publishing operations.
/// </summary>
public interface IBlogPublishingService
{
    /// <summary>
    /// Publishes a blog post to the configured repository.
    /// </summary>
    /// <param name="request">The blog post publishing request containing the content and commit message.</param>
    /// <returns>A <see cref="BlogPublishingResponse"/> indicating the result of the operation.</returns>
    Task<BlogPublishingResponse> PublishBlogPostAsync(BlogPostPublishRequest request);
}
