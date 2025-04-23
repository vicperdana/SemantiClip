namespace SemanticClip.Core.Models;

public class BlogPostProcessingResponse
{
    public List<string> BlogPosts { get; set; } = new();
    public VideoProcessingResponse VideoProcessingResponse { get; set; } = new();
    // number of times the blog post has been updated
    public int UpdateIndex { get; set; } = 0;
}