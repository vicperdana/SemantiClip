using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SemanticClip.Services.Utils;

namespace SemanticClip.Services.Services;

public class AzureAIAgentService : IAzureAIAgentService
{
    private readonly AgentsClient? _agentsClient;
    private readonly string? _chatModelId;
    private readonly ILogger<AzureAIAgentService>? _logger;

    public bool IsConfigured { get; }

    public AzureAIAgentService(
        string connectionString,
        string chatModelId,
        string vectorStoreId,
        ILogger<AzureAIAgentService>? logger = null)
    {
        IsConfigured = !string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(chatModelId);
        
        if (IsConfigured)
        {
            _agentsClient = new AgentsClient(connectionString, new DefaultAzureCredential());
            _chatModelId = chatModelId;
        }
        
        _logger = logger;
    }

    public async Task<string> EvaluateAsync(
        string blogPost,
        string instructions,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _agentsClient == null || _chatModelId == null)
        {
            _logger?.LogWarning("Azure AI Agent is not configured, returning original blog post");
            return blogPost;
        }

        _logger?.LogInformation("Evaluating blog post using Azure AI Agent");

        Agent? agent = null;
        bool agentCreated = false;
        try
        {
            // Load and parse YAML template
            string evaluateBlogPostYaml = EmbeddedResource.Read("EvaluateBlogPost.yaml");
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var yamlData = deserializer.Deserialize<Dictionary<string, object>>(evaluateBlogPostYaml);
            
            // Extract template and replace placeholder
            string template = yamlData["template"].ToString() ?? string.Empty;
            string promptWithBlogPost = template.Replace("{{$blogPost}}", blogPost);
            
            string agentName = yamlData.ContainsKey("name") ? yamlData["name"].ToString() ?? "EvaluateBlogPost" : "EvaluateBlogPost";
            string description = yamlData.ContainsKey("description") ? yamlData["description"].ToString() ?? string.Empty : string.Empty;
            
            // Create Azure AI agent
            var agentResponse = await _agentsClient.CreateAgentAsync(
                model: _chatModelId,
                name: agentName,
                instructions: description,
                cancellationToken: cancellationToken);
            agent = agentResponse.Value;
            agentCreated = true;

            _logger?.LogInformation("Created agent: {AgentId}", agent.Id);

            // Create thread (pass empty list for initial messages)
            var threadResponse = await _agentsClient.CreateThreadAsync(
                messages: new List<ThreadMessageOptions>(),
                cancellationToken: cancellationToken);
            var thread = threadResponse.Value;

            _logger?.LogInformation("Created thread: {ThreadId}", thread.Id);

            // Create message with prompt containing blog post
            await _agentsClient.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: promptWithBlogPost,
                cancellationToken: cancellationToken);

            _logger?.LogInformation("Created message with blog post content");

            // Run the agent
            var runResponse = await _agentsClient.CreateRunAsync(
                threadId: thread.Id,
                assistantId: agent.Id,
                cancellationToken: cancellationToken);

            _logger?.LogInformation("Started agent run: {RunId}", runResponse.Value.Id);

            // Wait for completion
            while (runResponse.Value.Status == RunStatus.Queued || 
                   runResponse.Value.Status == RunStatus.InProgress)
            {
                await Task.Delay(1000, cancellationToken);
                runResponse = await _agentsClient.GetRunAsync(
                    threadId: thread.Id,
                    runId: runResponse.Value.Id,
                    cancellationToken: cancellationToken);
            }

            _logger?.LogInformation("Agent run completed with status: {Status}", runResponse.Value.Status);

            // Get messages
            var messagesResponse = await _agentsClient.GetMessagesAsync(
                threadId: thread.Id,
                cancellationToken: cancellationToken);

            var messages = messagesResponse.Value;
            var assistantMessage = messages.Data
                .Where(m => m.Role == MessageRole.Agent)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            if (assistantMessage?.ContentItems?.Count > 0)
            {
                var textContent = assistantMessage.ContentItems
                    .OfType<MessageTextContent>()
                    .FirstOrDefault();

                if (textContent != null)
                {
                    _logger?.LogInformation("Retrieved evaluation result");
                    return textContent.Text;
                }
            }

            _logger?.LogWarning("No assistant response found, returning original blog post");
            return blogPost;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error evaluating blog post with Azure AI Agent");
            return blogPost; // Return original on error
        }
        finally
        {
            // Cleanup: delete the agent
            if (agentCreated && agent != null && _agentsClient != null)
            {
                try
                {
                    await _agentsClient.DeleteAgentAsync(agent.Id, cancellationToken);
                    _logger?.LogInformation("Deleted agent: {AgentId}", agent.Id);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to delete agent: {AgentId}", agent.Id);
                }
            }
        }
    }
}

public class NullAzureAIAgentService : IAzureAIAgentService
{
    public bool IsConfigured => false;

    public Task<string> EvaluateAsync(string blogPost, string instructions, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(blogPost); // Return unchanged
    }
}