using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticClip.Services.Services;
using SemanticClip.Services.Steps;

namespace SemanticClip.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services)
    {
        // Register the KernelService with its dependencies
        services.AddScoped<IKernelService, KernelService>();
        
        // Register the GenerateBlogPostStep
        services.AddTransient<GenerateBlogPostStep>();
        
        // Ensure logging is configured
        services.AddLogging();
        
        return services;
    }
} 