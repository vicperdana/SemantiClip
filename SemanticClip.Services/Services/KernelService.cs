using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Services.Plugins;

namespace SemanticClip.Services.Services;

public interface IKernelService
{
    Kernel CreateKernel();
}

public class KernelService : IKernelService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KernelService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public KernelService(
        IConfiguration configuration, 
        ILogger<KernelService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Kernel CreateKernel()
    {
        try
        {
            var builder = Kernel.CreateBuilder();
            
            var deploymentName = _configuration["AzureOpenAI:DeploymentName"];
            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"];

            if (string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Azure OpenAI configuration is missing. Please check your appsettings.json file.");
            }

            // Add chat completion service
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey
            );

            // Add our services to the kernel's service provider
            builder.Services.AddSingleton(_serviceProvider);
            builder.Services.AddTransient<BlogPostPlugin>();

            return builder.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create kernel");
            throw;
        }
    }
} 