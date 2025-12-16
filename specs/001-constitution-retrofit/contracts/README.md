# API Contracts: Constitution Retrofit - .NET 10 & Aspire Migration

**Date**: 2025-12-16  
**Purpose**: Document API contracts introduced or modified by this feature

---

## N/A - Infrastructure Migration

This feature is an **infrastructure migration** to .NET 10 and Aspire orchestration. No API contracts are added, removed, or modified.

### Existing API Endpoints (Unchanged)

All endpoints in **SemanticClip.API/Controllers/** remain functionally identical post-migration:

#### VideoProcessingController
- `POST /api/VideoProcessing/upload` - Video upload and processing initiation
- `GET /api/VideoProcessing/status/{id}` - Processing status check
- `GET /api/VideoProcessing/result/{id}` - Retrieve processing result

#### BlogPublishingController
- `POST /api/BlogPublishing/publish` - Publish blog to GitHub

#### ExportController
- `GET /api/Export/pdf` - Export blog content as PDF
- `GET /api/Export/markdown` - Export blog content as Markdown

### Service Discovery Impact

**Before Migration**:
```csharp
// Client hardcoded URL in appsettings.json
{
  "ApiBaseUrl": "https://localhost:7233"
}
```

**After Migration**:
```csharp
// Client uses service discovery with named service
builder.Services.AddHttpClient<VideoProcessingApiClient>(client => 
    client.BaseAddress = new Uri("http://api"));
```

**Contract Compatibility**: 100% backward compatible. API endpoint paths and request/response schemas unchanged. Only client-side discovery mechanism modified.

### OpenAPI Specification

Existing OpenAPI/Swagger configuration in **SemanticClip.API/Program.cs** remains active:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

Swagger UI continues to be available at `/swagger` endpoint for API documentation.

---

## Summary

**No API contract changes.** This migration preserves all existing HTTP endpoints, request/response formats, and error codes. Service discovery is a client-side networking change transparent to API contracts.
