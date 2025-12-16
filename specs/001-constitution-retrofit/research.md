# Research: Constitution Retrofit - .NET 10 & Aspire Migration

**Date**: 2025-12-16  
**Purpose**: Resolve technical unknowns identified in plan.md Technical Context section

---

## Research Task 1: .NET 10 & Aspire Latest Stable Versions

### Decision: Use .NET 10 with .NET Aspire 13.0.2 (Latest Stable)

### Rationale

As of December 2025, the latest stable versions are:
- **.NET 10**: Latest LTS runtime released November 2025
- **.NET Aspire 13.0.2**: Latest stable release (https://github.com/dotnet/aspire/releases/tag/v13.0.2), provides production-ready orchestration, service discovery, and observability

**Key Aspire 13.x Capabilities**:
- Built-in OpenTelemetry integration (traces, metrics, logs)
- Service discovery via `Microsoft.Extensions.ServiceDiscovery` package
- Aspire Dashboard for unified telemetry visualization
- Support for Redis, PostgreSQL, RabbitMQ, SQL Server, Azure services resource integrations
- Enhanced health checks and resilience patterns

**Version Compatibility**:
- Aspire requires .NET 8.0+ (SemantiClip currently on .NET 8.0)
- .NET 10 brings performance improvements and C# 13 features
- Aspire 10.x packages align with .NET 10 release cadence

### Alternatives Considered

1. **Stay on .NET 8.0 with earlier Aspire versions**
   - **Rejected**: Constitution v1.1.1 mandates .NET 10
   - Misses latest performance improvements
   - Shorter support window (8.0 is not LTS)

2. **Upgrade to .NET 10 without Aspire**
   - **Rejected**: Constitution explicitly requires Aspire for orchestration and observability
   - Manual OpenTelemetry configuration more complex
   - No unified dashboard for local development

3. **Wait for future Aspire versions**
   - **Rejected**: Aspire 13.0.2 is current stable with .NET 10 support
   - Delays constitutional compliance unnecessarily

### Implementation Details

**NuGet Packages to Add**:
```xml
<!-- AppHost Project -->
<PackageReference Include="Aspire.Hosting.AppHost" Version="13.0.2" />

<!-- ServiceDefaults Project -->
<PackageReference Include="Aspire.Hosting.ServiceDefaults" Version="13.0.2" />
<PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="13.0.*" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.*" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.*" />

<!-- API and Client Projects -->
<ProjectReference Include="../SemanticClip.ServiceDefaults/SemanticClip.ServiceDefaults.csproj" />
```

**SDK Version (global.json)**:
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
```

**Target Framework (all .csproj)**:
```xml
<TargetFramework>net10.0</TargetFramework>
```

---

## Research Task 2: Service Discovery Implementation Pattern

### Decision: Use `services.AddServiceDiscovery()` with HTTP client integration

### Rationale

Aspire service discovery eliminates hardcoded URLs by resolving service endpoints at runtime using named services defined in AppHost.

**Pattern**:
1. AppHost defines service names: `.AddProject<Projects.SemanticClip_API>("api")`
2. Client uses `HttpClient` with service name: `http://api`
3. Service discovery middleware resolves `http://api` → actual endpoint (e.g., `http://localhost:5000`)

**Benefits**:
- Zero configuration changes when moving from local → deployed environments
- Automatic failover if multiple instances registered
- Integrates with OpenTelemetry for distributed tracing

### Implementation Details

**AppHost Program.cs**:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.SemanticClip_API>("api");
var client = builder.AddProject<Projects.SemanticClip_Client>("client")
    .WithReference(api); // Injects "api" service endpoint into client

builder.Build().Run();
```

**Client Program.cs**:
```csharp
builder.Services.AddServiceDiscovery(); // Enable service discovery
builder.Services.AddHttpClient<VideoProcessingApiClient>(client => 
    client.BaseAddress = new Uri("http://api")); // Named service reference
```

**API Program.cs**:
```csharp
builder.AddServiceDefaults(); // Adds health checks, OpenTelemetry, service discovery
```

---

## Research Task 3: OpenTelemetry Configuration Best Practices

### Decision: Use Aspire ServiceDefaults extension pattern for centralized telemetry setup

### Rationale

ServiceDefaults project provides single source of truth for OpenTelemetry configuration, avoiding duplication across API and Client projects.

**Standard Pattern**:
```csharp
// ServiceDefaults/Extensions.cs
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging => 
            logging.IncludeFormattedMessage = true);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => 
                metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => 
                tracing.AddOtlpExporter());
        }

        return builder;
    }
}
```

**Telemetry Sources to Instrument**:
- ASP.NET Core requests (automatic)
- HttpClient calls (automatic)
- FFmpeg process execution (custom ActivitySource)
- Azure OpenAI API calls (via Semantic Kernel instrumentation)

---

## Research Task 4: FFmpeg Telemetry Integration

### Decision: Wrap FFmpeg execution in custom OpenTelemetry Activity

### Rationale

FFmpeg is critical path operation; failures must be captured in distributed traces for observability.

**Pattern**:
```csharp
private static readonly ActivitySource ActivitySource = new("SemanticClip.FFmpeg");

public async Task<string> ExtractAudioAsync(string videoPath)
{
    using var activity = ActivitySource.StartActivity("FFmpeg.ExtractAudio");
    activity?.SetTag("video.path", videoPath);
    
    try
    {
        var process = StartFFmpegProcess(args);
        await process.WaitForExitAsync();
        
        activity?.SetTag("ffmpeg.exit_code", process.ExitCode);
        if (process.ExitCode != 0)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "FFmpeg failed");
        }
        
        return outputPath;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

**Registration** (in Program.cs):
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("SemanticClip.FFmpeg")
        .AddSource("SemanticClip.*")); // Wildcard for all custom sources
```

---

## Research Task 5: Aspire Dashboard Access & Configuration

### Decision: Use default Aspire Dashboard configuration with optional authentication

### Rationale

Aspire Dashboard launches automatically when AppHost starts, providing zero-config observability for local development.

**Default Configuration**:
- **URL**: `http://localhost:15888` (auto-assigned)
- **Access**: Unauthenticated by default in development
- **Telemetry**: OTLP endpoint auto-configured for all services with ServiceDefaults

**Optional Security** (for shared development environments):
```json
// AppHost/appsettings.json
{
  "Dashboard": {
    "EnableAuth": true,
    "User": "dev",
    "Password": "<secure-value>"
  }
}
```

**Production**: Dashboard should NOT be deployed to production; use Azure Monitor / Application Insights for production telemetry.

---

## Research Task 6: Migration Path & Risk Mitigation

### Decision: Phased migration with validation gates

### Rationale

Minimize risk of breaking existing functionality by validating at each step.

**Phase Sequence**:

1. **Phase A: .NET 10 Upgrade**
   - Update global.json, all .csproj TargetFramework
   - Run `dotnet build` → GATE: Must build successfully
   - Run video processing workflow → GATE: Must complete successfully
   - Risk: Low (runtime-compatible upgrade)

2. **Phase B: Aspire Projects Setup**
   - Create AppHost and ServiceDefaults projects
   - Add to solution, verify build
   - Risk: None (new projects, no existing code changes)

3. **Phase C: ServiceDefaults Integration**
   - Reference ServiceDefaults in API and Client
   - Add `builder.AddServiceDefaults()` calls
   - Run services independently → GATE: Existing functionality works
   - Risk: Low (additive telemetry configuration)

4. **Phase D: Aspire Orchestration**
   - Configure AppHost to launch API and Client
   - Test via `dotnet run --project SemanticClip.AppHost`
   - Risk: Medium (new startup path; fallback: run projects directly)

5. **Phase E: Service Discovery**
   - Replace hardcoded Client → API URLs with service discovery
   - Test Client → API communication
   - Risk: Medium (network configuration change; fallback: restore hardcoded URLs)

**Rollback Strategy**:
- Feature branch allows reverting entire migration if blocking issues found
- Each phase commits separately for granular rollback
- Existing `dotnet run` commands remain functional during migration

---

## Summary of Research Findings

| Unknown | Resolution |
|---------|------------|
| Aspire version | .NET Aspire 13.0.2 (latest stable, supports .NET 10) |
| Service discovery pattern | `AddServiceDiscovery()` with HttpClient named services |
| OpenTelemetry setup | Centralized in ServiceDefaults using Aspire conventions |
| FFmpeg telemetry | Custom ActivitySource with automatic trace correlation |
| Dashboard configuration | Default localhost:15888, unauthenticated in dev |
| Migration risk | Low-Medium; phased approach with validation gates |

**All NEEDS CLARIFICATION items resolved. Proceeding to Phase 1 design.**
