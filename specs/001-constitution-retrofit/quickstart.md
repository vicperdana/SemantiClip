# Quickstart: Constitution Retrofit - .NET 10 & Aspire Migration

**Date**: 2025-12-16  
**Purpose**: Developer setup and run instructions after migration completion

---

## Prerequisites

### Required Software

1. **.NET 10 SDK** (10.0.100 or later)
   ```bash
   # Verify installation
   dotnet --version
   # Expected output: 10.0.100 or higher
   ```

2. **.NET Aspire Workload**
   ```bash
   # Install Aspire workload
   dotnet workload install aspire
   
   # Verify installation
   dotnet workload list
   # Should include: aspire
   ```

3. **Docker Desktop** (for Aspire Dashboard)
   - Required for Aspire Dashboard container
   - Download: https://www.docker.com/products/docker-desktop

4. **FFmpeg** (existing requirement, unchanged)
   ```bash
   # macOS
   brew install ffmpeg
   
   # Verify installation
   ffmpeg -version
   ```

### Configuration

**Azure OpenAI** (existing requirement, unchanged):
- Configure `SemanticClip.API/appsettings.json`:
  ```json
  {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-api-key",
      "WhisperDeploymentName": "whisper",
      "ContentDeploymentName": "gpt-4o"
    }
  }
  ```

**GitHub MCP** (if using blog publishing):
- Configure GitHub token in `appsettings.json` per existing DEPLOYMENT.md instructions

---

## Running the Application

### Option 1: Aspire Orchestration (Recommended)

**Single command to start all services:**

```bash
cd /Users/vicperdana/coderepo/sideprojects/SemantiClip
dotnet run --project SemanticClip.AppHost
```

**What happens:**
1. AppHost starts Aspire Dashboard
2. SemanticClip.API launches on dynamically assigned port
3. SemanticClip.Client launches and discovers API via service discovery
4. Dashboard opens automatically in browser at `http://localhost:15888`

**Expected Output:**
```
info: Aspire.Hosting.DistributedApplication[0]
      Aspire version: 13.0.2
info: Aspire.Hosting.DistributedApplication[0]
      Distributed application starting.
info: Aspire.Hosting.DistributedApplication[0]
      Dashboard running on http://localhost:15888
info: Aspire.Hosting.DistributedApplication[0]
      Now listening on: http://localhost:5000 (api)
info: Aspire.Hosting.DistributedApplication[0]
      Now listening on: http://localhost:5001 (client)
```

**Accessing Services:**
- **Aspire Dashboard**: http://localhost:15888 (telemetry, logs, traces)
- **API Swagger**: http://localhost:5000/swagger (port may vary)
- **Client UI**: http://localhost:5001 (port may vary; check Dashboard for actual URL)

### Option 2: Individual Projects (Fallback)

If you need to run services independently for debugging:

**Terminal 1 - API:**
```bash
cd SemanticClip.API
dotnet run
```

**Terminal 2 - Client:**
```bash
cd SemanticClip.Client
dotnet run
```

**Note**: When running individually, service discovery is bypassed. Client will use fallback URL configuration from `appsettings.json`.

---

## Verifying the Migration

### 1. Build Verification

```bash
dotnet build SemanticClip.sln
```

**Expected**: Zero errors, all 7 projects build successfully (Core, Infrastructure, Services, API, Client, AppHost, ServiceDefaults)

### 2. Target Framework Check

```bash
grep -r "TargetFramework" --include="*.csproj"
```

**Expected**: All projects show `<TargetFramework>net10.0</TargetFramework>`

### 3. Aspire Dashboard Access

1. Run: `dotnet run --project SemanticClip.AppHost`
2. Open browser: http://localhost:15888
3. Verify "Resources" tab shows:
   - ✅ **api** (Running)
   - ✅ **client** (Running)

### 4. Service Discovery Test

1. In Aspire Dashboard, navigate to "api" resource
2. Note the actual API port (e.g., `http://localhost:5000`)
3. Navigate to Client UI
4. Upload a test video
5. In Dashboard "Traces" tab, verify distributed trace shows:
   - Client → API HTTP request
   - API → FFmpeg activity
   - API → Azure OpenAI transcription

### 5. End-to-End Workflow Test

**Acceptance Test** (from spec.md User Story 1, Scenario 2):

```
Given: The solution is running on .NET 10 via AppHost
When: Video processing workflow is executed (upload video → transcribe → generate blog)
Then: Transcription and blog generation complete successfully
```

**Steps:**
1. Open Client UI (check Dashboard for URL)
2. Upload test video file (e.g., `test-video.mp4`)
3. Monitor processing status
4. Verify blog content is generated
5. Check Aspire Dashboard "Traces" for complete workflow trace

---

## Troubleshooting

### Issue: "Aspire Dashboard not accessible"

**Symptoms**: Browser shows "Connection refused" at http://localhost:15888

**Solutions**:
1. Verify Docker Desktop is running: `docker ps`
2. Check if port 15888 is in use: `lsof -i :15888`
3. Restart AppHost: `Ctrl+C` and re-run `dotnet run --project SemanticClip.AppHost`

### Issue: "Service discovery fails, Client cannot reach API"

**Symptoms**: Client shows "Failed to connect to API" errors

**Solutions**:
1. Verify AppHost logs show both services started
2. Check Dashboard "Resources" tab for service status
3. Fallback: Use individual project runs with hardcoded URLs

### Issue: ".NET 10 SDK not found"

**Symptoms**: `dotnet build` fails with "SDK version not found"

**Solutions**:
1. Install .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
2. Verify `global.json` in repo root specifies `"version": "10.0.100"`
3. Run `dotnet --list-sdks` to confirm installation

### Issue: "FFmpeg not found"

**Symptoms**: Video processing fails with "FFmpeg not found" error

**Solutions**:
1. Install FFmpeg: `brew install ffmpeg` (macOS)
2. Verify path in `appsettings.json` "FFmpeg:Path" (default: "ffmpeg" uses PATH)
3. Test: `ffmpeg -version`

### Issue: "OpenTelemetry traces not appearing in Dashboard"

**Symptoms**: Dashboard shows services running but no traces in "Traces" tab

**Solutions**:
1. Verify ServiceDefaults is referenced in API and Client projects
2. Check `Program.cs` in API and Client for `builder.AddServiceDefaults()` call
3. Trigger activity (e.g., API request) and refresh Dashboard
4. Check console logs for OpenTelemetry initialization messages

---

## Development Workflow

### Adding Telemetry to New Code

**Example: Adding custom activity for new feature**

```csharp
// 1. Create ActivitySource (singleton)
private static readonly ActivitySource ActivitySource = new("SemanticClip.FeatureName");

// 2. Wrap operation in activity
public async Task MyOperationAsync()
{
    using var activity = ActivitySource.StartActivity("FeatureName.Operation");
    activity?.SetTag("input.parameter", parameterValue);
    
    try
    {
        // Your code here
        var result = await DoWorkAsync();
        
        activity?.SetTag("result.count", result.Count);
        return result;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}

// 3. Register ActivitySource in Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("SemanticClip.FeatureName"));
```

### Viewing Logs, Traces, Metrics

**Aspire Dashboard Tabs:**
- **Resources**: Service health, ports, environment variables
- **Console Logs**: Real-time structured logs from all services
- **Traces**: Distributed traces with timing breakdown
- **Metrics**: Request rates, durations, error rates
- **Structured Logs**: Filterable JSON logs with context

---

## Next Steps

After verifying the migration:

1. **Run existing test suite** (when implemented per constitution roadmap)
2. **Update CHANGELOG.md** with migration details
3. **Create Pull Request** from `001-constitution-retrofit` branch
4. **Document any new configuration** in DEPLOYMENT.md for production
5. **Update developer onboarding docs** to reference this quickstart

---

## Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [SemanticClip Constitution](.specify/memory/constitution.md)
- [Feature Specification](./spec.md)
- [Research Findings](./research.md)
