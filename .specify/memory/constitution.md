<!--
================================================================================
SYNC IMPACT REPORT
================================================================================
Version Change: 1.1.0 → 1.1.1 (Patch: Runtime version update)

Added Sections: None

Modified Principles:
- Technology Stack: .NET 9 → .NET 10

Removed Sections: None

Templates Requiring Updates:
- .specify/templates/plan-template.md ✅ Compatible (no structural changes)
- .specify/templates/spec-template.md ✅ Compatible (no structural changes)
- .specify/templates/tasks-template.md ✅ Compatible (no structural changes)

Follow-up TODOs:
- Update global.json to target .NET 10 SDK
- Update all .csproj TargetFramework to net10.0
================================================================================
-->

# SemantiClip Constitution

## Core Principles

### I. Layered Architecture

All code MUST follow the established Clean Architecture pattern with clear separation of concerns:

- **SemanticClip.Core**: Domain models and interfaces only; zero external dependencies
- **SemanticClip.Infrastructure**: External service integrations (Azure OpenAI, FFmpeg, MCP)
- **SemanticClip.Services**: Business logic orchestration and AI workflow processing
- **SemanticClip.API**: HTTP endpoints and controllers; thin layer delegating to services
- **SemanticClip.Client**: Blazor WebAssembly UI; presentation logic only

**Rationale**: Enforces testability, maintainability, and enables swapping implementations
(e.g., switching from Ollama to another LLM) without affecting other layers.

### II. AI Workflow Modularity

AI processing pipelines MUST be implemented as discrete, composable steps using Microsoft Agent Framework:

- Each processing step (transcription, chapter generation, blog creation) MUST be independently executable
- Steps MUST communicate via well-defined contracts (input/output models in Core)
- New AI capabilities MUST be added as new steps, not by modifying existing ones
- Local LLM (Ollama) and cloud (Azure OpenAI) MUST be interchangeable via configuration

**Rationale**: Enables iterative enhancement, A/B testing of AI models, and graceful degradation
when specific services are unavailable.

### III. Configuration-Driven Integrations

All external service connections MUST be configurable without code changes:

- Azure OpenAI endpoints, API keys, and model deployments via `appsettings.json`
- GitHub MCP integration credentials via configuration
- FFmpeg paths and parameters via environment or configuration
- Ollama model selection via configuration
- Service URLs resolved via .NET Aspire service discovery (no hardcoded endpoints)

**Rationale**: Supports multiple deployment environments (local dev, staging, production) and
allows operators to tune AI models without redeployment. Aspire service discovery eliminates
URL management complexity and enables seamless local-to-cloud transitions.

### IV. Proof-of-Concept Boundaries

This project is explicitly a proof of concept; all contributors MUST acknowledge:

- Production-grade features (auth, rate limiting, caching) are roadmap items, not blockers
- Experimentation with new AI patterns is encouraged over premature optimization
- Breaking changes are acceptable during beta with proper CHANGELOG documentation
- Performance optimization deferred until core workflows are validated

**Rationale**: Enables rapid iteration and learning without over-engineering; prevents
scope creep while the core value proposition (video → blog content) is being validated.

### V. Observability First

All AI workflow operations MUST produce observable outputs:

- Structured logging for each processing step with timing information
- Error states MUST be captured with sufficient context for debugging
- Processing status MUST be exposed to the UI for user feedback
- FFmpeg operations MUST log command execution and exit codes
- .NET Aspire Dashboard MUST be used for local development observability
- OpenTelemetry traces, metrics, and logs via Aspire ServiceDefaults

**Rationale**: AI workflows are non-deterministic; observability is essential for debugging
failures and improving prompt engineering. Aspire provides unified telemetry out-of-the-box,
reducing instrumentation overhead.

## Technology Stack

The following technologies are authoritative for SemantiClip:

| Layer | Technology | Version/Notes |
|-------|------------|---------------|
| Runtime | .NET 10 | Latest runtime |
| Orchestration | .NET Aspire | Local dev orchestration, service discovery, observability |
| Frontend | Blazor WebAssembly | PWA-enabled |
| UI Components | MudBlazor | Material Design |
| AI Orchestration | Microsoft Agent Framework | Primary workflow engine |
| Transcription | Azure OpenAI Whisper | Cloud-based STT |
| Content Generation | Azure OpenAI GPT-4o / Ollama | Cloud or local LLM |
| Media Processing | FFmpeg | Audio extraction |
| GitHub Integration | ModelContextProtocol | Blog publishing |
| Infrastructure as Code | Bicep | Azure deployment |

### .NET Aspire Project Structure

| Project | Purpose |
|---------|----------|
| SemanticClip.AppHost | Aspire orchestration; defines service topology and dependencies |
| SemanticClip.ServiceDefaults | Shared configuration: OpenTelemetry, health checks, resilience |

**Constraint**: New dependencies MUST be justified against existing capabilities. Prefer
extending current integrations over adding new services.

## Development Workflow

### Branch Strategy

- `main`: Protected; represents stable releases
- Feature branches: `###-feature-name` format (e.g., `001-add-export-pdf`)
- All changes via Pull Request with description of changes

### Quality Gates

1. **Build**: `dotnet build` MUST pass with zero errors
2. **Format**: Code MUST follow .NET conventions (IDE suggestions)
3. **Changelog**: User-facing changes MUST be documented in `CHANGELOG.md`
4. **Security**: No secrets in code; configuration via environment/appsettings

### Testing Expectations (Roadmap)

Testing infrastructure is planned but not yet implemented. When added:
- Unit tests for Services layer business logic
- Integration tests for AI workflow pipelines
- Contract tests for API endpoints

## Governance

This constitution supersedes ad-hoc decisions and establishes the authoritative development
guidelines for SemantiClip.

**Amendment Process**:
1. Propose changes via Pull Request to `.specify/memory/constitution.md`
2. Document rationale for principle additions/modifications
3. Update `CHANGELOG.md` with governance changes
4. Increment constitution version per semantic versioning:
   - MAJOR: Principle removal or incompatible redefinition
   - MINOR: New principle or section added
   - PATCH: Clarifications and wording improvements

**Compliance**: All PRs SHOULD reference relevant principles when making architectural decisions.
Deviations MUST be justified in the PR description.

**Version**: 1.1.1 | **Ratified**: 2025-12-15 | **Last Amended**: 2025-12-16
