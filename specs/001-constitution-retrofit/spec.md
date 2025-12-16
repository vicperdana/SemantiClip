# Feature Specification: Constitution Retrofit - .NET 10 & Aspire Migration

**Feature Branch**: `001-constitution-retrofit`  
**Created**: 2025-12-16  
**Status**: Draft  
**Input**: User description: "retrofit existing tech stack to comply with the constitution"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - .NET 10 Runtime Migration (Priority: P1)

As a developer, I need the entire solution upgraded to .NET 10 so that the codebase complies with the constitutional requirement and benefits from the latest runtime performance and features.

**Why this priority**: Foundation for all other constitutional compliance work. Without .NET 10, we cannot add Aspire (which requires .NET 8+), and we're not meeting the core technology stack requirement.

**Independent Test**: Can be fully tested by running `dotnet build` and `dotnet run` on all projects after updating target frameworks. All existing functionality should work identically on .NET 10.

**Acceptance Scenarios**:

1. **Given** all projects are targeting .NET 8.0, **When** target frameworks are updated to net10.0, **Then** the solution builds successfully without errors
2. **Given** the solution is running on .NET 10, **When** video processing workflow is executed, **Then** transcription and blog generation complete successfully
3. **Given** global.json is updated to .NET 10 SDK, **When** `dotnet --version` is run, **Then** it shows .NET 10.x SDK

---

### User Story 2 - Aspire Orchestration Setup (Priority: P2)

As a developer, I need .NET Aspire orchestration configured so that I can run all services with unified observability and service discovery in local development.

**Why this priority**: Enables the constitutional requirement for Aspire Dashboard observability and service discovery. Must come after P1 (requires .NET 10 runtime).

**Independent Test**: Can be fully tested by running `dotnet run --project SemanticClip.AppHost` and verifying that the Aspire Dashboard opens, showing the API and Client services with health status and telemetry.

**Acceptance Scenarios**:

1. **Given** Aspire AppHost project exists, **When** `dotnet run --project SemanticClip.AppHost` is executed, **Then** Aspire Dashboard launches at http://localhost:15888
2. **Given** services are orchestrated by Aspire, **When** checking Dashboard, **Then** both SemanticClip.API and SemanticClip.Client appear with green health status
3. **Given** Aspire is managing services, **When** API receives a request, **When** checking Dashboard traces, **Then** OpenTelemetry traces are visible showing request flow

---

### User Story 3 - Service Discovery Integration (Priority: P3)

As a developer, I need the Blazor Client to discover the API endpoint via Aspire service discovery so that no hardcoded URLs exist and environment transitions are seamless.

**Why this priority**: Implements Principle III (Configuration-Driven Integrations) requirement for service discovery. Depends on P2 (Aspire must be running).

**Independent Test**: Can be fully tested by changing the API port in AppHost, restarting services, and verifying the Client automatically connects to the new port without configuration changes.

**Acceptance Scenarios**:

1. **Given** Client is using Aspire service discovery, **When** Client makes API calls, **Then** requests succeed without hardcoded URLs
2. **Given** API port is changed in AppHost configuration, **When** services restart, **Then** Client automatically discovers the new API endpoint
3. **Given** services are running via Aspire, **When** API is unavailable, **Then** Client shows appropriate error without connection timeout issues

---

### User Story 4 - OpenTelemetry & Observability (Priority: P4)

As a developer, I need OpenTelemetry traces, metrics, and logs integrated via Aspire ServiceDefaults so that AI workflow debugging uses the constitutional observability-first approach.

**Why this priority**: Implements Principle V (Observability First) with Aspire's built-in telemetry. Requires P2 (ServiceDefaults project).

**Independent Test**: Can be fully tested by processing a video and viewing the complete trace in Aspire Dashboard, showing all processing steps (audio extraction, transcription, blog generation) with timing information.

**Acceptance Scenarios**:

1. **Given** ServiceDefaults is configured, **When** video processing starts, **Then** a distributed trace appears in Dashboard spanning all workflow steps
2. **Given** OpenTelemetry is active, **When** FFmpeg executes, **Then** logs include command execution and exit codes visible in Dashboard
3. **Given** an error occurs during transcription, **When** checking Dashboard, **Then** error context and stack trace are captured in telemetry

---

### Edge Cases

- What happens when .NET 10 SDK is not installed on developer machine?
- How does system handle Aspire Dashboard not being accessible (port conflict)?
- What if service discovery fails during Client startup?
- How are existing appsettings.json endpoints handled during transition to service discovery?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST target .NET 10 (net10.0) in all project TargetFramework properties
- **FR-002**: System MUST use .NET 10 SDK as specified in global.json
- **FR-003**: System MUST include SemanticClip.AppHost project for Aspire orchestration
- **FR-004**: System MUST include SemanticClip.ServiceDefaults project for shared OpenTelemetry configuration
- **FR-005**: SemanticClip.API MUST reference ServiceDefaults and register OpenTelemetry
- **FR-006**: SemanticClip.Client MUST reference ServiceDefaults and register OpenTelemetry
- **FR-007**: AppHost MUST register SemanticClip.API as a project resource
- **FR-008**: AppHost MUST register SemanticClip.Client as a project resource
- **FR-009**: SemanticClip.Client MUST use Aspire service discovery to locate API endpoint
- **FR-010**: ServiceDefaults MUST configure OpenTelemetry with traces, metrics, and logs
- **FR-011**: ServiceDefaults MUST configure health checks for registered services
- **FR-012**: All existing functionality (video processing, transcription, blog generation, GitHub publishing) MUST work identically after migration
- **FR-013**: FFmpeg operations MUST emit telemetry logs visible in Aspire Dashboard
- **FR-014**: Aspire Dashboard MUST be accessible at default endpoint during local development

### Key Entities *(N/A - infrastructure change, no new domain entities)*

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developer can run entire solution using single command (`dotnet run --project SemanticClip.AppHost`)
- **SC-002**: All services start successfully within 10 seconds of AppHost execution
- **SC-003**: Aspire Dashboard displays all services with green health status within 15 seconds
- **SC-004**: Video processing workflow completes in same time as before migration (within 5% variance)
- **SC-005**: OpenTelemetry traces capture end-to-end workflow with all processing steps visible
- **SC-006**: Developer can view logs, traces, and metrics for all services in single unified Dashboard
- **SC-007**: Service discovery eliminates need for manual API endpoint configuration in Client

## Assumptions

- .NET 10 SDK is available and installed on developer machines
- Aspire templates are available via `dotnet new install Aspire.ProjectTemplates`
- Existing Azure OpenAI, Ollama, FFmpeg, and GitHub MCP integrations remain unchanged
- Current development environment is macOS (per workspace context)
- Migration occurs on feature branch; no production impact during development

## Dependencies

- .NET 10 SDK installation
- Aspire workload installation (`dotnet workload install aspire`)
- No breaking changes expected in current NuGet dependencies when retargeting to net10.0

## Out of Scope

- Authentication/authorization implementation (roadmap item per Principle IV)
- Unit/integration test creation (roadmap item per constitution)
- Performance optimization beyond observability (deferred per Principle IV)
- Azure deployment configuration changes (Bicep remains unchanged)
- Migration of third-party libraries to newer versions (unless required for .NET 10 compatibility)
