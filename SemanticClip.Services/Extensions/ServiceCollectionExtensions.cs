using Microsoft.Extensions.DependencyInjection;
using SemanticClip.Services.Services;

namespace SemanticClip.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services)
    {
        services.AddScoped<IKernelService, KernelService>();
        return services;
    }
} 