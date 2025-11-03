using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using SemanticClip.Core.Models;
using SemanticClip.Services.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SemanticClip.Services.Executors;

/// <summary>
/// Third executor: generates blog post from transcript using Azure OpenAI
/// </summary>
public sealed class GenerateBlogPostExecutor : Executor<string, BlogPostProcessingResponse>
{
    private readonly ILogger<GenerateBlogPostExecutor> _logger;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly IConfiguration _configuration;
    
    public GenerateBlogPostExecutor(
        ILogger<GenerateBlogPostExecutor> logger,
        AzureOpenAIClient openAIClient,
        IConfiguration configuration) 
        : base("GenerateBlogPostExecutor")
    {
        _logger = logger;
        _openAIClient = openAIClient;
        _configuration = configuration;
    }
    
    public override async ValueTask<BlogPostProcessingResponse> HandleAsync(
        string transcript, 
        IWorkflowContext context)
    {
        _logger.LogInformation("Generating blog post from transcript");
        
        try
        {
            // Load YAML template
            string yamlTemplate = EmbeddedResource.Read("GenerateBlogPost.yaml");
            var promptConfig = ParseYamlTemplate(yamlTemplate);
            
            // Get chat client from injected Azure OpenAI client
            var chatClient = _openAIClient.GetChatClient(promptConfig.Model ?? "gpt-4o");
            
            // Build messages with template
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(promptConfig.Instructions ?? "You are a helpful blog post writer."),
                new UserChatMessage(promptConfig.UserPrompt?.Replace("{{$transcript}}", transcript) ?? transcript)
            };
            
            // Generate blog post
            _logger.LogInformation("Calling Azure OpenAI to generate blog post");
            var response = await chatClient.CompleteChatAsync(
                messages, 
                cancellationToken: CancellationToken.None);
            
            string blogPost = response.Value.Content[0].Text;
            
            _logger.LogInformation("Blog post generated: {Length} characters", blogPost.Length);
            
            // Build response
            var videoProcessingResponse = new VideoProcessingResponse
            {
                Transcript = transcript,
                BlogPost = blogPost
            };
            
            return new BlogPostProcessingResponse
            {
                BlogPosts = new List<string> { blogPost },
                VideoProcessingResponse = videoProcessingResponse,
                UpdateIndex = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating blog post");
            throw;
        }
    }
    
    private PromptConfig ParseYamlTemplate(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            var yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yaml);
            
            return new PromptConfig
            {
                Name = yamlObject.ContainsKey("name") ? yamlObject["name"].ToString() : "BlogPostGenerator",
                Model = yamlObject.ContainsKey("model") ? yamlObject["model"].ToString() : "gpt-4o",
                Instructions = yamlObject.ContainsKey("template") ? yamlObject["template"].ToString() : 
                              yamlObject.ContainsKey("description") ? yamlObject["description"].ToString() : null,
                UserPrompt = yamlObject.ContainsKey("template") ? yamlObject["template"].ToString() : 
                            "Generate a blog post from this transcript: {{$transcript}}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse YAML template, using defaults");
            return new PromptConfig
            {
                Name = "BlogPostGenerator",
                Model = "gpt-4o",
                Instructions = "You are a helpful assistant that generates engaging blog posts from video transcripts.",
                UserPrompt = "Generate a blog post from this transcript: {{$transcript}}"
            };
        }
    }
}

internal record PromptConfig
{
    public string? Name { get; init; }
    public string? Model { get; init; }
    public string? Instructions { get; init; }
    public string? UserPrompt { get; init; }
}
