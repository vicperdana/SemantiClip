using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using SemanticClip.Core.Models;
using SemanticClip.Services.Services;
using SemanticClip.Services.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SemanticClip.Services.Executors;

/// <summary>
/// Fourth executor: evaluates blog post quality using Azure AI Agent (optional step)
/// </summary>
public sealed class EvaluateBlogPostExecutor : Executor<BlogPostProcessingResponse, VideoProcessingResponse>
{
    private readonly ILogger<EvaluateBlogPostExecutor> _logger;
    private readonly IAzureAIAgentService? _azureAIService;
    
    public EvaluateBlogPostExecutor(
        ILogger<EvaluateBlogPostExecutor> logger,
        IAzureAIAgentService? azureAIService = null) 
        : base("EvaluateBlogPostExecutor")
    {
        _logger = logger;
        _azureAIService = azureAIService;
    }
    
    public override async ValueTask<VideoProcessingResponse> HandleAsync(
        BlogPostProcessingResponse input, 
        IWorkflowContext context)
    {
        _logger.LogInformation("Evaluating blog post quality");
        
        try
        {
            // Check if Azure AI Agent is configured
            if (_azureAIService == null || !_azureAIService.IsConfigured)
            {
                _logger.LogWarning("Azure AI Agent not configured, skipping blog post evaluation");
                return input.VideoProcessingResponse;
            }
            
            // Load evaluation template
            string yamlTemplate = EmbeddedResource.Read("EvaluateBlogPost.yaml");
            var promptConfig = ParseYamlTemplate(yamlTemplate);
            
            // Evaluate using Azure AI Agent
            string blogPost = input.BlogPosts[input.UpdateIndex];
            string instructions = promptConfig.Instructions ?? "Evaluate this blog post for quality, relevance, and engagement.";
            
            _logger.LogInformation("Invoking Azure AI Agent for blog post evaluation");
            string evaluatedPost = await _azureAIService.EvaluateAsync(
                blogPost, 
                instructions,
                CancellationToken.None);
            
            _logger.LogInformation("Blog post evaluation completed");
            
            return new VideoProcessingResponse
            {
                BlogPost = evaluatedPost,
                Transcript = input.VideoProcessingResponse.Transcript
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating blog post, returning original");
            // Return original blog post if evaluation fails
            return input.VideoProcessingResponse;
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
                Name = yamlObject.ContainsKey("name") ? yamlObject["name"].ToString() : "BlogEvaluator",
                Instructions = yamlObject.ContainsKey("template") ? yamlObject["template"].ToString() : 
                              yamlObject.ContainsKey("description") ? yamlObject["description"].ToString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse YAML template, using defaults");
            return new PromptConfig
            {
                Name = "BlogEvaluator",
                Instructions = "You are a blog post evaluator. Evaluate the quality, relevance, and engagement level of the provided blog post."
            };
        }
    }
    
    private record PromptConfig
    {
        public string? Name { get; init; }
        public string? Instructions { get; init; }
    }
}
