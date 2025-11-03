using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SemanticClip.Client.Services
{
    public class ExportApiClient
    {
        private readonly HttpClient _httpClient;
        public ExportApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<byte[]> ExportDocxAsync(string content, string fileName)
        {
            var request = new
            {
                MarkdownContent = content,
                Content = content, // Backward compatibility
                FileName = fileName
            };
            var response = await _httpClient.PostAsJsonAsync("api/Export/docx", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<byte[]> ExportAsync(string content, string fileName, string format, string? pageSize = null)
        {
            var request = new
            {
                MarkdownContent = content,
                Content = content,
                FileName = fileName,
                Format = format,
                PageSize = pageSize
            };
            var response = await _httpClient.PostAsJsonAsync("api/Export/export", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
