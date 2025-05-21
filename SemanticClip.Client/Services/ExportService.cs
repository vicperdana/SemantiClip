using Microsoft.JSInterop;
using SemanticClip.Core.Enums;
using System.Text;
using System.Threading.Tasks;

namespace SemanticClip.Client.Services;

public class ExportService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ExportService> _logger;

    public ExportService(IJSRuntime jsRuntime, ILogger<ExportService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task ExportContentAsync(string content, string fileName, ExportFormat format)
    {
        try
        {
            switch (format)
            {
                case ExportFormat.TXT:
                    await ExportToTextAsync(content, fileName);
                    break;
                case ExportFormat.HTML:
                    await ExportToHtmlAsync(content, fileName);
                    break;
                case ExportFormat.PDF:
                    await ExportToPdfAsync(content, fileName);
                    break;
                case ExportFormat.DOCX:
                    // DOCX export would typically require a server-side component
                    // For client-side, we'll fall back to a basic text export for now
                    await ExportToTextAsync(content, $"{fileName}.docx");
                    break;
                default:
                    await ExportToTextAsync(content, fileName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting content");
            throw;
        }
    }

    private async Task ExportToTextAsync(string content, string fileName)
    {
        if (!fileName.EndsWith(".txt"))
        {
            fileName += ".txt";
        }
        
        await _jsRuntime.InvokeVoidAsync("exportHelpers.saveAsTextFile", fileName, content);
    }

    private async Task ExportToHtmlAsync(string content, string fileName)
    {
        if (!fileName.EndsWith(".html"))
        {
            fileName += ".html";
        }
        
        await _jsRuntime.InvokeVoidAsync("exportHelpers.saveAsHtml", fileName, content);
    }

    private async Task ExportToPdfAsync(string content, string fileName)
    {
        if (!fileName.EndsWith(".pdf"))
        {
            fileName += ".pdf";
        }
        
        // For now, we'll use a basic approach to create a text-only PDF
        // In a production app, you might want to use a more sophisticated library
        // or server-side generation
        await _jsRuntime.InvokeVoidAsync("alert", "PDF export functionality is coming soon. Exporting as text for now.");
        await ExportToTextAsync(content, fileName.Replace(".pdf", ".txt"));
    }
}