using Microsoft.SemanticKernel.Process;
using SemanticClip.Core.Models;

namespace SemanticClip.Services.Steps;

#pragma warning disable SKEXP0080
public class CompletionStep : KernelProcessStep<VideoProcessingResponse>
{
    internal VideoProcessingResponse? _state;
    
    public override ValueTask ActivateAsync(KernelProcessStepState<VideoProcessingResponse> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }

    public static class Functions
    {
        public const string CompleteProcessing = nameof(CompleteProcessing);
    }

    [KernelFunction(Functions.CompleteProcessing)]
    public async Task<VideoProcessingResponse> CompleteProcessingAsync(string transcript, KernelProcessStepContext context)
    {
        if (_state == null)
        {
            _state = new VideoProcessingResponse();
        }

        _state.Status = "Completed";
        _state.Transcript = transcript;
        _state.Chapters = new List<Chapter>();
        _state.BlogPost = "";
        
        await context.EmitEventAsync("Completed", _state);
        
        return _state;
    }
} 