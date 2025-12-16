# Data Model: Constitution Retrofit - .NET 10 & Aspire Migration

**Date**: 2025-12-16  
**Purpose**: Document entities and data structures introduced or modified by this feature

---

## N/A - Infrastructure Migration

This feature is an **infrastructure migration** to .NET 10 and Aspire orchestration. No new domain entities, data models, or database schemas are introduced.

### Existing Domain Models (Unchanged)

The following entities in **SemanticClip.Core/Models/** remain unchanged:

- **VideoProcessingRequest**: User input for video upload and processing
- **TranscriptionResult**: Output from Azure OpenAI Whisper API
- **BlogContent**: Generated blog post with metadata
- **GitHubPublishRequest**: Blog publishing parameters

### Configuration Models (New)

While not domain entities, the following configuration structures are added for Aspire orchestration:

#### AppHost Service Registration
```csharp
// Defined in SemanticClip.AppHost/Program.cs
// Represents service topology, not persisted data

var api = builder.AddProject<Projects.SemanticClip_API>("api")
    .WithReplicas(1);

var client = builder.AddProject<Projects.SemanticClip_Client>("client")
    .WithReference(api)
    .WithReplicas(1);
```

#### Telemetry Context
```csharp
// OpenTelemetry Activity tags (not persisted)
// Captured in distributed traces via Aspire Dashboard

Activity tags:
- "video.path": string
- "ffmpeg.exit_code": int
- "transcription.duration": TimeSpan
- "blog.word_count": int
```

### State Transitions (Unchanged)

Existing video processing workflow state machine remains unchanged:
1. **Uploaded** → 2. **Extracting Audio** → 3. **Transcribing** → 4. **Generating Content** → 5. **Complete**

Aspire telemetry adds visibility into these transitions but does not modify the state model.

---

## Validation Rules (Unchanged)

- Video file size limits: Defined in `appsettings.json` "FileUpload:MaxRequestBodySizeInBytes"
- Allowed video formats: `.mp4`, `.avi`, `.mov`, `.wmv`, `.mkv`
- FFmpeg timeout: Configurable via `appsettings.json` "FFmpeg:TimeoutMinutes"

No new validation rules introduced by Aspire migration.

---

## Summary

**No data model changes required.** This migration is purely infrastructure-focused, adding orchestration and observability without altering domain logic or data structures. Existing models in SemanticClip.Core remain authoritative.
