<!--
================================================================================
SYNC IMPACT REPORT
================================================================================
Version Change: N/A → 1.0.0 (Initial constitution)

Added Sections:
- Core Principles (I–V)
- Technology Stack section
- Development Workflow section
- Governance section

Modified Principles: N/A (initial version)
Removed Sections: N/A (initial version)

Templates Requiring Updates:
- .specify/templates/plan-template.md ✅ Compatible (Constitution Check section exists)
- .specify/templates/spec-template.md ✅ Compatible (Requirements/testing align)
- .specify/templates/tasks-template.md ✅ Compatible (phase structure matches workflow)

Follow-up TODOs: None
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

**Rationale**: Supports multiple deployment environments (local dev, staging, production) and
allows operators to tune AI models without redeployment.

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

**Rationale**: AI workflows are non-deterministic; observability is essential for debugging
failures and improving prompt engineering.

## Technology Stack

The following technologies are authoritative for SemantiClip:

| Layer | Technology | Version/Notes |
|-------|------------|---------------|
| Runtime | .NET 9 | LTS preferred when available |
| Frontend | Blazor WebAssembly | PWA-enabled |
| UI Components | MudBlazor | Material Design |
| AI Orchestration | Microsoft Agent Framework | Primary workflow engine |
| Transcription | Azure OpenAI Whisper | Cloud-based STT |
| Content Generation | Azure OpenAI GPT-4o / Ollama | Cloud or local LLM |
| Media Processing | FFmpeg | Audio extraction |
| GitHub Integration | ModelContextProtocol | Blog publishing |
| Infrastructure as Code | Bicep | Azure deployment |

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

**Version**: 1.0.0 | **Ratified**: 2025-12-15 | **Last Amended**: 2025-12-15
