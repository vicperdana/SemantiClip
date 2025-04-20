using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using SemanticClip.Core.Services;
using SemanticClip.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using SemanticClip.Services.Plugins;
using SemanticClip.Services.Steps;
using Microsoft.Extensions.Logging;
using SemanticClip.Services.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Register services
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();

// Configure logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
});

// Register all the Semantic Kernel process steps
builder.Services.AddTransient<PrepareVideoStep>();
builder.Services.AddTransient<TranscribeVideoStep>();
builder.Services.AddTransient<GenerateBlogPostStep>();

// Register BlogPostPlugin with proper logger
builder.Services.AddTransient<BlogPostPlugin>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<BlogPostPlugin>>();
    return new BlogPostPlugin(logger);
});

// Register KernelService
builder.Services.AddScoped<IKernelService, KernelService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
app.UseCors("AllowAll");

// Enable WebSockets
app.UseWebSockets();

app.UseAuthorization();
app.MapControllers();

app.Run();

