using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Plugins;

public sealed class PublishBlogPlugin
{
    private readonly ILogger _logger;
    
    public PublishBlogPlugin(ILogger logger)
    {
        _logger = logger;
    }

    [KernelFunction, Description("Publish the blog post to GitHub using ModelContextProtocol client with GitHub as server.")]
    public async Task<VideoProcessingResponse> PublishBlogPluginAsync(
        Kernel kernel,
        [Description("The blog post to be published.")]
        string blogPost)
    {
       
        _logger.LogInformation("Publishing the blog post: {blogPost}", blogPost);
        
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var chat = new ChatHistory();

        chat.AddSystemMessage("You are a coordinator for a blog post publishing process. " +
                              "You will receive a blog post and you need to publish it to GitHub.");
        chat.AddUserMessage($"Publish this blog post:\nBlog Post:\n{blogPost}\n");

        var response = await chatCompletion.GetChatMessageContentAsync(chat);
        var publishedBlogPost = response.Content;
        _logger.LogInformation("Published blog post: {BlogPost}", publishedBlogPost);
        
        return new VideoProcessingResponse
        {
            Status = "Completed",
            Transcript = "",
            BlogPost = publishedBlogPost!
        };
    }
} 