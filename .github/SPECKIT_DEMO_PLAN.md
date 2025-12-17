# SpecKit Agent Framework Demo Plan

## Overview
This document outlines a comprehensive demonstration of SpecKit's agent-driven development workflow using SemantiClip's migration to .NET 10 and latest .NET Aspire as a showcase example.

## Demo Context
**Project**: SemantiClip - AI-powered video-to-blog content generator  
**Current State**: Successfully migrated to Microsoft Agent Framework with MCP integration  
**Demo Objective**: Showcase full SpecKit workflow from constitution to phased implementation  
**Target Upgrade**: .NET 10 + Latest .NET Aspire Release

---

## Phase 1: Setup & Constitution

### Step 1.1: Branch Creation
```bash
# Switch to the feature branch for agent framework migration
git checkout feature/agent-framework-migration

# Verify we're on the correct branch
git status
```

**Expected Outcome**: Working in `feature/agent-framework-migration` branch

### Step 1.2: Initialize SpecKit
```bash
# Initialize SpecKit in the repository
specify init --here
```

**Expected Outcome**: 
- `.specify/` directory created with templates
- Agent configuration files generated
- SpecKit ready for use

### Step 1.3: Generate Constitution
```bash
# Open VSCode (or continue in terminal with agent mode)
# In Agent Mode, invoke:
@speckit.constitution specify constitution for this code base, ensure .NET 10 and latest Aspire release is mandated going forward
```

**Expected Outcome**:
- `.specify/memory/constitution.md` created or updated
- Core principles defined:
  - Layered Architecture (Clean Architecture)
  - AI Workflow Modularity (Microsoft Agent Framework)
  - Configuration-Driven Integrations
  - Proof-of-Concept Boundaries
  - Observability First
- Technology stack updated to .NET 10 and latest Aspire
- .NET Aspire project structure defined (AppHost, ServiceDefaults)

### Step 1.4: Review & Commit Constitution
```bash
# Review the generated constitution
cat .specify/memory/constitution.md

# Commit the constitution
git add .specify/memory/constitution.md
git commit -m "feat(constitution): establish v2.0.0 with .NET 10 & Aspire mandate"
git push origin feature/agent-framework-migration
```

**Commit Message Convention**: `feat(constitution): [description]`

---

## Phase 2: Specification & Analysis

### Step 2.1: Analyze Current State
```bash
# Run the analyze agent to understand current state
@speckit.analyze analyze the codebase against the constitution
```

**Expected Outcome**:
- Gap analysis report
- Identification of .NET 9 → .NET 10 migration points
- Aspire integration requirements
- Compliance status with constitutional principles

### Step 2.2: Specify Feature Requirements
```bash
# Generate detailed specification
@speckit.specify create specification for .NET 10 and Aspire upgrade following constitutional guidelines
```

**Expected Outcome**:
- `specs/{feature-id}/spec.md` created with:
  - Motivation: Why upgrade to .NET 10 & Aspire
  - Requirements: Technical and architectural needs
  - Acceptance Criteria: Success metrics
  - Dependencies: Service discovery, observability, etc.
  - Non-Goals: Out of scope items
- Reference to constitution principles

### Step 2.3: Review & Commit Specification
```bash
# Review generated specification
cat specs/{feature-id}/spec.md

# Commit the specification
git add specs/{feature-id}/
git commit -m "feat(spec): .NET 10 and Aspire upgrade specification"
git push origin feature/agent-framework-migration
```

---

## Phase 3: Planning & Task Breakdown

### Step 3.1: Generate Implementation Plan
```bash
# Create detailed implementation plan
@speckit.plan create implementation plan for .NET 10 and Aspire upgrade
```

**Expected Outcome**:
- `specs/{feature-id}/plan.md` created with:
  - Phased approach (multiple phases)
  - Risk assessment and mitigation
  - Dependencies and prerequisites
  - Rollback strategy
- Timeline estimates for each phase

### Step 3.2: Break Down into Tasks
```bash
# Generate task list from plan
@speckit.tasks create tasks from the implementation plan
```

**Expected Outcome**:
- `specs/{feature-id}/tasks.md` created with:
  - Phase 1 Tasks: Foundation & SDK Update
    - Update global.json to .NET 10 SDK
    - Update all .csproj TargetFramework to net10.0
    - Update NuGet package references
  - Phase 2 Tasks: Aspire Integration
    - Create SemanticClip.AppHost project
    - Create SemanticClip.ServiceDefaults project
    - Configure service discovery
    - Implement OpenTelemetry integration
  - Phase 3 Tasks: Service Migration
    - Migrate API project to Aspire
    - Migrate Client project to Aspire
    - Update configuration for service discovery
  - Phase 4 Tasks: Validation & Documentation
    - Test all workflows
    - Update README and docs
    - Validate observability

### Step 3.3: Generate Checklists
```bash
# Create implementation checklists
@speckit.checklist generate checklists for each phase
```

**Expected Outcome**:
- `specs/{feature-id}/checklists/phase-{n}-checklist.md` for each phase
- Pre-flight checks
- Implementation steps
- Verification steps
- Success criteria validation

### Step 3.4: Review & Commit Plan and Tasks
```bash
# Review plan and tasks
cat specs/{feature-id}/plan.md
cat specs/{feature-id}/tasks.md

# Commit planning artifacts
git add specs/{feature-id}/plan.md specs/{feature-id}/tasks.md specs/{feature-id}/checklists/
git commit -m "feat(plan): phased implementation plan for .NET 10 & Aspire"
git push origin feature/agent-framework-migration
```

---

## Phase 4: Phased Implementation

### Phase 4.1: Implement Phase 1 - Foundation & SDK Update

```bash
# Start implementation of Phase 1
@speckit.implement implement phase 1 tasks following the checklist
```

**Implementation Steps**:
1. Update `global.json` with .NET 10 SDK version
2. Update all `.csproj` files to `<TargetFramework>net10.0</TargetFramework>`
3. Update NuGet packages to compatible versions
4. Build and verify no breaking changes

**Expected Files Changed**:
- `global.json`
- `SemanticClip.Core/SemanticClip.Core.csproj`
- `SemanticClip.Services/SemanticClip.Services.csproj`
- `SemanticClip.API/SemanticClip.API.csproj`
- `SemanticClip.Client/SemanticClip.Client.csproj`

**Verification**:
```bash
# Build solution
dotnet build

# Run existing workflows (if tests exist)
dotnet test
```

**Commit**:
```bash
git add global.json **/*.csproj
git commit -m "feat(phase-1): upgrade to .NET 10 SDK and runtime"
git push origin feature/agent-framework-migration
```

### Phase 4.2: Implement Phase 2 - Aspire Integration

```bash
# Continue with Phase 2
@speckit.implement implement phase 2 tasks for Aspire infrastructure
```

**Implementation Steps**:
1. Create `SemanticClip.AppHost` project
   ```bash
   dotnet new aspire-apphost -n SemanticClip.AppHost
   ```
2. Create `SemanticClip.ServiceDefaults` project
   ```bash
   dotnet new aspire-servicedefaults -n SemanticClip.ServiceDefaults
   ```
3. Configure service topology in AppHost
4. Add OpenTelemetry and health checks in ServiceDefaults

**Expected Files Created**:
- `SemanticClip.AppHost/Program.cs`
- `SemanticClip.AppHost/SemanticClip.AppHost.csproj`
- `SemanticClip.ServiceDefaults/Extensions.cs`
- `SemanticClip.ServiceDefaults/SemanticClip.ServiceDefaults.csproj`

**Verification**:
```bash
# Run Aspire AppHost
cd SemanticClip.AppHost
dotnet run

# Verify Aspire dashboard accessible at http://localhost:15888
```

**Commit**:
```bash
git add SemanticClip.AppHost/ SemanticClip.ServiceDefaults/
git commit -m "feat(phase-2): add Aspire orchestration projects"
git push origin feature/agent-framework-migration
```

### Phase 4.3: Implement Phase 3 - Service Migration

```bash
# Continue with Phase 3
@speckit.implement implement phase 3 tasks for service migration to Aspire
```

**Implementation Steps**:
1. Add ServiceDefaults reference to API and Client projects
2. Configure service discovery in AppHost
3. Update API Program.cs to use ServiceDefaults
4. Update Client Program.cs to use ServiceDefaults
5. Remove hardcoded URLs, use service discovery

**Expected Files Changed**:
- `SemanticClip.API/Program.cs`
- `SemanticClip.API/SemanticClip.API.csproj`
- `SemanticClip.Client/Program.cs`
- `SemanticClip.Client/SemanticClip.Client.csproj`
- `SemanticClip.AppHost/Program.cs` (service registrations)

**Verification**:
```bash
# Run via Aspire
cd SemanticClip.AppHost
dotnet run

# Test transcription workflow
# Test blog generation workflow
# Verify telemetry in Aspire dashboard
```

**Commit**:
```bash
git add SemanticClip.API/ SemanticClip.Client/ SemanticClip.AppHost/
git commit -m "feat(phase-3): migrate services to Aspire with service discovery"
git push origin feature/agent-framework-migration
```

### Phase 4.4: Implement Phase 4 - Validation & Documentation

```bash
# Final phase implementation
@speckit.implement implement phase 4 validation and documentation updates
```

**Implementation Steps**:
1. Update `README.md` with Aspire setup instructions
2. Update `DEPLOYMENT.md` with Aspire deployment guidance
3. Add `docs/aspire-guide.md` for local development
4. Update `CHANGELOG.md` with all changes
5. Run full integration tests

**Expected Files Changed**:
- `README.md`
- `DEPLOYMENT.md`
- `CHANGELOG.md`
- `docs/aspire-guide.md` (new)

**Verification**:
```bash
# Full workflow test
cd SemanticClip.AppHost
dotnet run

# Upload test video
# Verify transcription
# Verify blog generation
# Verify MCP GitHub publishing
# Check Aspire dashboard for traces and metrics
```

**Commit**:
```bash
git add README.md DEPLOYMENT.md CHANGELOG.md docs/
git commit -m "docs(phase-4): update documentation for .NET 10 & Aspire"
git push origin feature/agent-framework-migration
```

---

## Phase 5: Background Agent Showcase

### Step 5.1: Enable Background Agent Mode
```bash
# Start background agent for continuous monitoring
@speckit.analyze --watch monitor codebase for constitutional compliance
```

**Background Agent Features**:
- **Continuous Compliance Monitoring**: Watches for code changes that violate constitution
- **Automatic Documentation Updates**: Suggests doc updates when code changes
- **Dependency Security Scanning**: Alerts on vulnerable packages
- **Architecture Drift Detection**: Flags layer boundary violations

### Step 5.2: Demonstrate Agent Feedback
```bash
# Make an intentional architecture violation
# Example: Add database code directly in API controller

# Background agent should alert:
# ⚠️ Constitutional Violation Detected
# Principle: I. Layered Architecture
# Issue: Data access logic in API layer
# Recommendation: Move to Infrastructure layer
```

### Step 5.3: Automated Task Creation
```bash
# Convert background agent findings to tasks
@speckit.taskstoissues create GitHub issues from agent findings
```

**Expected Outcome**:
- GitHub issues created automatically
- Tagged with `speckit-agent`, `architecture`, `constitution-compliance`
- Assigned to appropriate team members

---

## Phase 6: Integration & Pull Request

### Step 6.1: Final Validation
```bash
# Run full validation suite
dotnet build --configuration Release
dotnet test

# Verify Aspire deployment
cd SemanticClip.AppHost
dotnet run

# Check all constitutional principles satisfied
@speckit.analyze final compliance check
```

### Step 6.2: Create Pull Request
```bash
# Push all changes
git push origin feature/agent-framework-migration

# Create PR via GitHub CLI or web interface
gh pr create \
  --title "feat: Upgrade to .NET 10 and latest .NET Aspire" \
  --body "$(cat specs/{feature-id}/spec.md)" \
  --base main
```

**PR Description Template**:
```markdown
## Summary
Completes migration to .NET 10 and latest .NET Aspire following constitutional mandate.

## Constitutional Compliance
- ✅ Layered Architecture maintained
- ✅ Configuration-driven (Aspire service discovery)
- ✅ Observability first (OpenTelemetry via Aspire)
- ✅ All phases completed and validated

## Changes
- Upgraded SDK to .NET 10
- Added Aspire orchestration (AppHost, ServiceDefaults)
- Implemented service discovery
- Integrated OpenTelemetry
- Updated documentation

## Testing
- [x] Build passes
- [x] Manual workflow testing
- [x] Aspire dashboard validation
- [x] Documentation reviewed

## References
- Specification: `specs/{feature-id}/spec.md`
- Implementation Plan: `specs/{feature-id}/plan.md`
- Tasks: `specs/{feature-id}/tasks.md`
```

---

## Key Demo Highlights

### 1. **AI-Driven Specification**
- Constitution acts as "project DNA"
- Agents understand context and constraints
- Consistent architecture decisions

### 2. **Phased Implementation**
- Each phase independently committable
- Clear rollback points
- Incremental value delivery

### 3. **Background Intelligence**
- Continuous compliance monitoring
- Proactive issue detection
- Automated task management

### 4. **Full Traceability**
- Constitution → Spec → Plan → Tasks → Code
- Every change justified by specification
- Audit trail for architectural decisions

### 5. **Collaboration Friendly**
- Clear handoffs between phases
- Checklists for non-technical validation
- GitHub integration for team coordination

---

## Success Metrics

- ✅ **Constitution adherence**: 100% of phases reference constitutional principles
- ✅ **Commit hygiene**: Each phase committed separately with semantic messages
- ✅ **Documentation completeness**: README, DEPLOYMENT, and guides updated
- ✅ **Build success**: All phases build without errors
- ✅ **Observability**: Aspire dashboard shows traces and metrics
- ✅ **Service discovery**: No hardcoded URLs in configuration
- ✅ **Agent effectiveness**: Background agent catches at least one violation

---

## Timeline

| Phase | Duration | Effort |
|-------|----------|--------|
| Phase 1: Setup & Constitution | 30 min | 2 hrs agent-assisted |
| Phase 2: Specification & Analysis | 20 min | 1 hr agent-assisted |
| Phase 3: Planning & Task Breakdown | 20 min | 1 hr agent-assisted |
| Phase 4.1: Foundation & SDK Update | 30 min | 2 hrs manual + agent verification |
| Phase 4.2: Aspire Integration | 1 hr | 4 hrs manual + agent guidance |
| Phase 4.3: Service Migration | 1 hr | 4 hrs manual + agent guidance |
| Phase 4.4: Validation & Documentation | 30 min | 2 hrs manual + agent review |
| Phase 5: Background Agent Showcase | 15 min | Continuous monitoring |
| Phase 6: Integration & PR | 15 min | 1 hr review |
| **Total** | **~4.5 hrs** | **~17 hrs traditional development** |

---

## Notes & Observations

### Lessons Learned (Post-Demo)
- Record observations here during actual demo execution
- Note any agent suggestions that were particularly helpful
- Document any constitution updates triggered by implementation

### Known Issues
- Background agent requires active session (runs in IDE)
- GitHub integration requires PAT with appropriate scopes
- Aspire dashboard requires local port 15888 available

### Future Enhancements
- Automated PR description generation from spec
- Agent-driven code review checklist
- Integration with CI/CD for constitutional validation gates

---

## Appendix: Command Reference

### Essential SpecKit Commands
```bash
# Initialize SpecKit
specify init --here

# Agent invocations (in VSCode or terminal)
@speckit.constitution   # Create/update constitution
@speckit.analyze       # Analyze codebase compliance
@speckit.specify       # Generate feature specification
@speckit.plan          # Create implementation plan
@speckit.tasks         # Break down into tasks
@speckit.checklist     # Generate phase checklists
@speckit.implement     # Guide implementation
@speckit.clarify       # Ask clarifying questions
@speckit.taskstoissues # Convert tasks to GitHub issues

# Background monitoring
@speckit.analyze --watch
```

### Git Workflow Commands
```bash
# Branch management
git checkout feature/agent-framework-migration
git status
git log --oneline

# Stage and commit
git add <files>
git commit -m "type(scope): description"
git push origin feature/agent-framework-migration

# PR creation
gh pr create --title "..." --body "..."
```

### .NET Commands
```bash
# Build and test
dotnet build
dotnet test

# Aspire projects
dotnet new aspire-apphost -n ProjectName.AppHost
dotnet new aspire-servicedefaults -n ProjectName.ServiceDefaults

# Run Aspire
cd ProjectName.AppHost
dotnet run
```

---

**Demo Plan Version**: 1.0.0  
**Created**: 2025-12-17  
**Last Updated**: 2025-12-17  
**Target Audience**: Development teams, stakeholders, AI-assisted workflow advocates
