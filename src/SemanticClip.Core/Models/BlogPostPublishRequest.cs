using System;

namespace SemanticClip.Core.Models
{
    /// <summary>
    /// Represents a request to publish a blog post with MCP
    /// </summary>
    public class BlogPostPublishRequest
    {
        /// <summary>
        /// The blog post content to publish
        /// </summary>
        public string? BlogPost { get; set; }
        
        /// <summary>
        /// The commit message for the publication
        /// </summary>
        public string? CommitMessage { get; set; }
    }
}
