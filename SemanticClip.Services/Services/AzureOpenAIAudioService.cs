using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SemanticClip.Services.Services;

public class AzureOpenAIAudioService : IAudioTranscriptionService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAIAudioService> _logger;

    public AzureOpenAIAudioService(
        AzureOpenAIClient client,
        IConfiguration configuration,
        ILogger<AzureOpenAIAudioService> logger)
    {
        _client = client;
        _deploymentName = configuration["AzureOpenAI:WhisperDeploymentName"] 
            ?? throw new InvalidOperationException("Whisper deployment name not configured");
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Transcribing audio file: {AudioPath}", audioFilePath);
        
        using var audioStream = File.OpenRead(audioFilePath);
        
        var audioClient = _client.GetAudioClient(_deploymentName);
        var result = await audioClient.TranscribeAudioAsync(audioStream, audioFilePath, cancellationToken: cancellationToken);
        
        _logger.LogInformation("Transcription completed: {Length} characters", result.Value.Text.Length);
        
        return result.Value.Text;
    }
}
