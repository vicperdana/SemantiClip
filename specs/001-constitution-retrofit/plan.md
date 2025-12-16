# Implementation Plan: Constitution Retrofit - .NET 10 & Aspire Migration

**Branch**: `001-constitution-retrofit` | **Date**: 2025-12-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-constitution-retrofit/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Retrofit the SemantiClip solution to comply with constitutional technology stack requirements by migrating from .NET 8.0 to .NET 10 and integrating .NET Aspire orchestration. This migration establishes the foundation for service discovery, unified observability via OpenTelemetry, and streamlined local development with Aspire Dashboard. The technical approach involves updating all project target frameworks, adding AppHost and ServiceDefaults projects, configuring service discovery for the Blazor Client to discover the API endpoint, and integrating OpenTelemetry telemetry throughout the application stack.

## Technical Context

**Language/Version**: C# .NET 10 (target migration from .NET 8.0)  
**Primary Dependencies**: 
- Current: ASP.NET Core (API), Blazor WebAssembly (Client), Azure OpenAI SDK, Microsoft.SemanticKernel, FFmpeg, MudBlazor
- To Add: .NET Aspire 13.x (orchestration), Aspire.Hosting 13.0.*, Microsoft.Extensions.ServiceDiscovery 13.0.*, OpenTelemetry 1.9.*  
**Aspire Version**: 13.0.2 (latest stable, resolved in research.md)
**Storage**: Azure Blob Storage (video uploads), local file system (temporary audio extraction), Azure AI Search (vector embeddings)  
**Testing**: No formal test infrastructure yet (per constitution roadmap item)  
**Target Platform**: 
- Development: macOS (per workspace context)
- Production: Azure (Container Apps via Bicep IaC, per DEPLOYMENT.md)
- Client: Browser (Blazor WASM as PWA)  
**Project Type**: Web application (ASP.NET Core backend + Blazor WASM frontend)  
**Performance Goals**: 
- Video processing workflow completion time maintained (within 5% of current)
- Service startup within 10 seconds of AppHost execution
- Aspire Dashboard responsiveness within 15 seconds
**Constraints**: 
- All existing functionality must work identically post-migration (FR-012)
- No breaking changes to Azure deployment (Bicep unchanged per Out of Scope)
- FFmpeg integration must remain functional
- Azure OpenAI and Ollama integrations unchanged
**Scale/Scope**: 
- 5 projects (Core, Infrastructure, Services, API, Client) + 2 new Aspire projects (AppHost, ServiceDefaults)
- ~50+ source files across solution
- Single-tenant proof-of-concept (per Principle IV)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Layered Architecture ✅ COMPLIANT
- **Assessment**: Migration maintains existing layer separation. AppHost and ServiceDefaults are infrastructure additions that don't violate boundaries.
- **Impact**: SemanticClip.Core remains dependency-free. ServiceDefaults will be referenced by API and Client for telemetry configuration, which is appropriate cross-cutting concern.

### Principle II: AI Workflow Modularity ✅ COMPLIANT
- **Assessment**: No changes to AI workflow structure. Microsoft Agent Framework integration remains unchanged.
- **Impact**: Aspire observability will enhance visibility into existing modular steps without modifying them.

### Principle III: Configuration-Driven Integrations ✅ COMPLIANT (Enhanced)
- **Assessment**: Migration strengthens this principle by adding Aspire service discovery, eliminating hardcoded API URLs in Client.
- **Impact**: Existing appsettings.json configurations preserved. Service discovery configuration added to AppHost.

### Principle IV: Proof-of-Concept Boundaries ✅ COMPLIANT
- **Assessment**: Migration aligns with PoC status. Aspire is experimental/learning-oriented infrastructure.
- **Impact**: No production-grade features added; focus remains on observability and developer experience.

### Principle V: Observability First ✅ COMPLIANT (Primary Goal)
- **Assessment**: Migration directly implements this principle via OpenTelemetry and Aspire Dashboard.
- **Impact**: Replaces basic structured logging with distributed tracing, metrics, and unified dashboard visualization.

### Technology Stack ⚠️ PARTIAL COMPLIANCE (Migration Goal)
- **Current State**: .NET 8.0 (non-compliant with constitution v1.1.1 requirement for .NET 10)
- **Post-Migration**: .NET 10 + Aspire (full compliance)
- **Gate Status**: PROCEED - Migration explicitly addresses this violation

### Development Workflow ✅ COMPLIANT
- **Assessment**: Migration follows branch strategy (001-constitution-retrofit), will require PR with CHANGELOG update.
- **Impact**: Build command remains `dotnet build`; adds new run command `dotnet run --project SemanticClip.AppHost`.

**GATE DECISION**: ✅ PROCEED TO PHASE 0
- No constitutional violations requiring justification
- Migration explicitly resolves Technology Stack non-compliance
- All principles either maintained or strengthened

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# Current Structure (preserved)
SemanticClip.Core/
├── Interfaces/
├── Models/
└── SemanticClip.Core.csproj

SemanticClip.Infrastructure/
├── [External service implementations]
└── [Azure OpenAI, FFmpeg integrations]

SemanticClip.Services/
├── Executors/
├── Extensions/
├── Resources/
├── Services/
├── Utilities/
├── Utils/
└── SemanticClip.Services.csproj

SemanticClip.API/
├── Controllers/
│   ├── BlogPublishingController.cs
│   ├── ExportController.cs
│   └── VideoProcessingController.cs
├── Properties/
│   └── launchSettings.json
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── SemanticClip.API.csproj

SemanticClip.Client/
├── Layout/
│   └── MainLayout.razor
├── Pages/
│   ├── Authentication.razor
│   ├── Index.razor
│   ├── Login.razor
│   └── Profile.razor
├── Services/
│   ├── CustomAuthenticationStateProvider.cs
│   ├── ExportApiClient.cs
│   └── VideoProcessingApiClient.cs
├── Shared/
├── wwwroot/
├── Program.cs
└── SemanticClip.Client.csproj

# New Aspire Projects (to be created)
SemanticClip.AppHost/                    # NEW
├── Program.cs                           # Aspire orchestration entry point
├── appsettings.json                     # AppHost configuration
└── SemanticClip.AppHost.csproj          # References: Aspire.Hosting.AppHost

SemanticClip.ServiceDefaults/            # NEW
├── Extensions.cs                        # OpenTelemetry, health check, resilience setup
└── SemanticClip.ServiceDefaults.csproj  # References: Aspire.Hosting.ServiceDefaults

# Infrastructure (unchanged)
infra/
├── main.bicep
└── main.parameters.json

# Root Configuration Files (to be modified)
global.json                              # Update SDK version to 10.0.x
SemanticClip.sln                         # Add AppHost and ServiceDefaults projects
```

**Structure Decision**: Web application with Clean Architecture layers. Migration adds two new Aspire orchestration projects (AppHost, ServiceDefaults) at solution root, following .NET Aspire conventions. Existing 5-project structure preserved. No test projects exist yet (per constitution roadmap).

## Complexity Tracking

> **No constitutional violations requiring justification.**

This migration adds 2 new projects (AppHost, ServiceDefaults) to the solution, increasing the total from 5 to 7 projects. This is justified by:
- Aspire architectural conventions require separate AppHost and ServiceDefaults projects
- Aligns with constitutional Principle V (Observability First) and Technology Stack requirements
- Standard .NET Aspire pattern; not custom complexity

---

## Phase 1 Post-Design Constitution Re-Check

**Re-evaluation Date**: 2025-12-16 (after research.md, data-model.md, contracts/, quickstart.md completed)

### Design Artifacts Review

✅ **research.md**: Resolves all NEEDS CLARIFICATION items
- Aspire version: 10.x (latest stable, aligned with .NET 10)
- Service discovery, OpenTelemetry, FFmpeg telemetry patterns documented
- Migration phased approach with rollback strategy

✅ **data-model.md**: Confirms no domain model changes (N/A for infrastructure migration)

✅ **contracts/**: Confirms 100% API contract backward compatibility

✅ **quickstart.md**: Provides developer onboarding with Aspire orchestration

### Constitutional Compliance After Design

| Principle | Initial Check | Post-Design Status | Notes |
|-----------|---------------|-------------------|-------|
| I. Layered Architecture | ✅ Compliant | ✅ Compliant | ServiceDefaults is cross-cutting infrastructure; appropriate layer separation maintained |
| II. AI Workflow Modularity | ✅ Compliant | ✅ Compliant | No changes to Microsoft Agent Framework workflows |
| III. Configuration-Driven | ✅ Compliant (Enhanced) | ✅ Compliant (Enhanced) | Service discovery eliminates hardcoded URLs; Aspire Dashboard configurable |
| IV. PoC Boundaries | ✅ Compliant | ✅ Compliant | No production features added; focus on observability learning |
| V. Observability First | ✅ Compliant (Primary Goal) | ✅ Compliant (Primary Goal) | OpenTelemetry + Aspire Dashboard fully implements principle |
| Technology Stack | ⚠️ Partial (Migration Goal) | ✅ Compliant (Post-Implementation) | .NET 10 + Aspire 13.0.2 specified in research.md |
| Development Workflow | ✅ Compliant | ✅ Compliant | Branch strategy, build commands, CHANGELOG update required |

### Risk Assessment Post-Design

**Low Risk Items**:
- .NET 10 runtime upgrade (backward compatible)
- Aspire project creation (additive, no existing code changes)
- ServiceDefaults integration (additive telemetry)

**Medium Risk Items** (mitigated by phased approach):
- Aspire orchestration as new startup path (fallback: existing `dotnet run` per project)
- Service discovery replacing hardcoded URLs (fallback: restore URLs in appsettings.json)

**No High Risk Items Identified**

### Gate Decision: PROCEED TO PHASE 2 (Task Planning)

**Justification**:
- All constitutional principles maintained or strengthened
- Design artifacts complete and internally consistent
- Migration path clearly defined with rollback strategies
- Developer experience improved (single command startup via AppHost)
- No breaking changes to existing functionality identified

**Next Step**: Execute `/speckit.tasks` command to generate tasks.md for implementation
