namespace SemanticClip.Services.Services;

public interface IAzureAIAgentService
{
    Task<string> EvaluateAsync(string blogPost, string instructions, CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
}
