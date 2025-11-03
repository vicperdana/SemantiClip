using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemanticClip.Services.Executors;
using SemanticClip.Services.Services;

namespace SemanticClip.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSemanticClipWorkflows(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Azure OpenAI Client
        services.AddSingleton(sp =>
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var apiKey = configuration["AzureOpenAI:ApiKey"];
            
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Azure OpenAI configuration is missing");
            }
            
            return new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
        });
        
        // Register audio transcription service wrapper
        services.AddSingleton<IAudioTranscriptionService, AzureOpenAIAudioService>();
        
        // Register Azure AI Agent service (if configured)
        services.AddSingleton<IAzureAIAgentService>(sp =>
        {
            var connectionString = configuration["AzureAIAgent:ConnectionString"];
            var chatModelId = configuration["AzureAIAgent:ChatModelId"];
            var vectorStoreId = configuration["AzureAIAgent:VectorStoreId"];
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(chatModelId))
            {
                // Return null implementation if not configured
                return new NullAzureAIAgentService();
            }
            
            return new AzureAIAgentService(connectionString, chatModelId, vectorStoreId ?? "default");
        });
        
        // Register workflow executors
        services.AddTransient<PrepareVideoExecutor>();
        services.AddTransient<TranscribeVideoExecutor>();
        services.AddTransient<GenerateBlogPostExecutor>();
        services.AddTransient<EvaluateBlogPostExecutor>();
        services.AddTransient<PublishBlogPostExecutor>();
        
        return services;
    }
}
