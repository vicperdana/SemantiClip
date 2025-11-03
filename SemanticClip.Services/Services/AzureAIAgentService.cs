using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace SemanticClip.Services.Services;

public class AzureAIAgentService : IAzureAIAgentService
{
    private readonly string _connectionString;
    private readonly string _chatModelId;
    private readonly string _vectorStoreId;
    private readonly ILogger<AzureAIAgentService>? _logger;

    public bool IsConfigured => !string.IsNullOrEmpty(_connectionString);

    public AzureAIAgentService(
        string connectionString,
        string chatModelId,
        string vectorStoreId,
        ILogger<AzureAIAgentService>? logger = null)
    {
        _connectionString = connectionString;
        _chatModelId = chatModelId;
        _vectorStoreId = vectorStoreId;
        _logger = logger;
    }

    public async Task<string> EvaluateAsync(
        string blogPost,
        string instructions,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Evaluating blog post using Azure AI Agent");

        // TODO: Update Azure.AI.Projects API usage once stable API documentation is available
        // The preview API has changed significantly between versions
        await Task.CompletedTask;
        
        return "Evaluation temporarily disabled - Azure.AI.Projects API in flux";
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
