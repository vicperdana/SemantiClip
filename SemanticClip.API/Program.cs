using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SemanticClip.Core.Interfaces;
using SemanticClip.Services;
using SemanticClip.Services.Services;
using SemanticClip.Services.Plugins;
using SemanticClip.Services.Steps;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Xabe.FFmpeg to download and use bundled FFmpeg binaries for Windows compatibility
var ffmpegPath = Path.Combine(builder.Environment.ContentRootPath, "ffmpeg");
Directory.CreateDirectory(ffmpegPath);

try
{
    // Download FFmpeg binaries at startup (will be cached after first download)
    // This automatically downloads Windows-compatible binaries when running on Windows
    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
    
    // Set the path for Xabe.FFmpeg
    FFmpeg.SetExecutablesPath(ffmpegPath);
    
    // Log successful setup
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("FFmpeg");
    logger.LogInformation("FFmpeg binaries downloaded and configured at: {Path}", ffmpegPath);
}
catch (Exception ex)
{
    // Log error but don't crash the application
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("FFmpeg");
    logger.LogError(ex, "Failed to download FFmpeg binaries. Video processing features may not work.");
}

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get the max request body size from configuration and convert to int safely
var maxRequestBodySize = (int)Math.Min(
    builder.Configuration.GetValue<long>("FileUpload:MaxRequestBodySizeInBytes", 3000000),
    int.MaxValue
);

// Configure request size limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxRequestBodySize;
});

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodySize;
});

// Configure IIS integration through Kestrel
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodySize;
});

// Add CORS - Allow both localhost and production domains
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7227",
                "http://localhost:5244", 
                "https://semanticlipweb.vicperdana.com",
                "https://semanticlip.vicperdana.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Using custom authentication
// Authentication is handled on the client side
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//         // Custom authentication implementation
//    });

// builder.Services.AddAuthorization();


// Register services
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
builder.Services.AddSingleton<IJobTrackingService, InMemoryJobTrackingService>();
builder.Services.AddScoped<IBlogPublishingService, BlogPublishingService>();

// Register BlogPublishingController-related services
builder.Services.AddTransient<PublishBlogPostStep>();

// Configure logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

// Register all the Semantic Kernel process steps
builder.Services.AddTransient<PrepareVideoStep>();
builder.Services.AddTransient<TranscribeVideoStep>();
builder.Services.AddTransient<GenerateBlogPostStep>();
builder.Services.AddTransient<EvaluateBlogPostStep>();
builder.Services.AddTransient<PublishBlogPostStep>();


// Register BlogPostPlugin with proper logger
builder.Services.AddTransient<BlogPostPlugin>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<BlogPostPlugin>>();
    return new BlogPostPlugin(logger);
});

// Register PublishBlogPlugin with proper logger
builder.Services.AddTransient<PublishBlogPlugin>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<PublishBlogPlugin>>();
    return new PublishBlogPlugin(logger);
});

// Register KernelService
//builder.Services.AddScoped<IKernelService, KernelService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for troubleshooting
app.UseSwagger();
app.UseSwaggerUI();

// Configure request size limits middleware
app.Use(async (context, next) =>
{
    var bodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (bodySizeFeature != null)
    {
        bodySizeFeature.MaxRequestBodySize = maxRequestBodySize;
    }
    await next();
});

app.UseHttpsRedirection();

// Use appropriate CORS policy based on environment
var corsPolicy = app.Environment.IsDevelopment() ? "AllowAll" : "AllowSpecificOrigins";
app.UseCors(corsPolicy);

// Authentication middleware removed - using custom authentication on client side
// app.UseAuthentication();
// app.UseAuthorization();
app.MapControllers();

app.Run();



