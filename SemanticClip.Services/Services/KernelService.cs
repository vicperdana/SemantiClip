using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticClip.Services.Services;

public interface IKernelService
{
    Kernel CreateKernel();
}

public class KernelService : IKernelService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KernelService> _logger;

    public KernelService(IConfiguration configuration, ILogger<KernelService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        
        // Add chat completion service
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: _configuration["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured"),
            endpoint: _configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured"),
            apiKey: _configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured")
        );

        return builder.Build();
    }
} 