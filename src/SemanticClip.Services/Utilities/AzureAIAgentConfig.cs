using System.ComponentModel.DataAnnotations;
namespace SemanticClip.Services.Utilities;
/// <summary>
/// Azure AI configuration settings.
/// </summary>
public static class AzureAIAgentConfig
{
    public static string ConnectionString { get; set; } = string.Empty;
    public static string ChatModelId { get; set; } = string.Empty;
    public static string BingConnectionId { get; set; } = string.Empty;
    public static string VectorStoreId { get; set; } = string.Empty;
    public static int MaxEvaluations { get; set; } = 3; // Default to 3 if not configured
}