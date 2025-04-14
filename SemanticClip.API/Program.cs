using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using SemanticClip.Core.Services;
using SemanticClip.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;

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

