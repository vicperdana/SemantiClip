using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using SemanticClip.Client;
using SemanticClip.Client.Services;
using SemanticClip.Core.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to point to the API
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "http://127.0.0.1:5290";
builder.Services.AddScoped(sp => new HttpClient { 
    BaseAddress = new Uri(apiBaseAddress),
    MaxResponseContentBufferSize = 3000000 // 3MB
});

// Add file upload configuration to client
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    { "FileUpload:MaxRequestBodySizeInBytes", "3000000" }
});

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});
builder.Services.AddMudMarkdownServices();

// Register VideoProcessingApiClient
builder.Services.AddScoped<VideoProcessingApiClient>();

await builder.Build().RunAsync();
