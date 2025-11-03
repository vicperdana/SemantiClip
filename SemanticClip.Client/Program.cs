using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MudBlazor.Services;
using SemanticClip.Client;
using SemanticClip.Client.Services;
using SemanticClip.Core.Interfaces;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to point to the API
var apiBaseAddress = builder.HostEnvironment.IsDevelopment() 
    ? "http://localhost:5290/" 
    : builder.Configuration["ApiBaseAddress"];

// Ensure trailing slash
if (!string.IsNullOrEmpty(apiBaseAddress) && !apiBaseAddress.EndsWith("/"))
{
    apiBaseAddress += "/";
}

// Add custom authentication
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// Configure HTTP client without access token support for now
builder.Services.AddScoped(sp =>
{
    // var handler = sp.GetRequiredService<BaseAddressAuthorizationMessageHandler>();
    // handler.ConfigureHandler(
    //     authorizedUrls: new[] { apiBaseAddress! },
    //     scopes: new[] { "openid", "profile", "email" });
    
    var httpClient = new HttpClient()
    {
        BaseAddress = new Uri(apiBaseAddress!),
        MaxResponseContentBufferSize = 3000000 // 3MB
    };
    return httpClient;
});

// Add file upload configuration to client
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
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

// Register VideoProcessingApiClient and ExportApiClient
builder.Services.AddScoped<VideoProcessingApiClient>();
builder.Services.AddScoped<ExportApiClient>();

await builder.Build().RunAsync();
