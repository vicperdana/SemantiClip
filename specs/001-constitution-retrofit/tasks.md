---
description: "Task list for Constitution Retrofit - .NET 10 & Aspire Migration"
---

# Tasks: Constitution Retrofit - .NET 10 & Aspire Migration

**Input**: Design documents from `/specs/001-constitution-retrofit/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not included (no formal test infrastructure per constitution roadmap)

**Organization**: Tasks are grouped by user story to enable independent implementation and validation of each story.

## Format: `- [ ] [ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- All tasks include exact file paths

## Path Conventions

- Repository root: `/Users/vicperdana/coderepo/sideprojects/SemantiClip/`
- Projects: `SemanticClip.{Core,Infrastructure,Services,API,Client,AppHost,ServiceDefaults}/`
- All paths are relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prerequisites and tooling verification

- [ ] T001 Verify .NET 10 SDK installed (run `dotnet --version`, expect 10.0.100+)
- [ ] T002 Install Aspire workload (run `dotnet workload install aspire`)
- [ ] T003 Verify Docker Desktop running (required for Aspire Dashboard)
- [ ] T004 Create backup branch checkpoint (run `git checkout -b 001-constitution-retrofit-backup`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Runtime and SDK upgrades that MUST be complete before Aspire integration

**⚠️ CRITICAL**: No Aspire work can begin until this phase is complete

- [ ] T005 Update global.json SDK version to 10.0.100 in global.json
- [ ] T006 [P] Update TargetFramework to net10.0 in SemanticClip.Core/SemanticClip.Core.csproj
- [ ] T007 [P] Update TargetFramework to net10.0 in SemanticClip.Infrastructure/SemanticClip.Infrastructure.csproj
- [ ] T008 [P] Update TargetFramework to net10.0 in SemanticClip.Services/SemanticClip.Services.csproj
- [ ] T009 [P] Update TargetFramework to net10.0 in SemanticClip.API/SemanticClip.API.csproj
- [ ] T010 [P] Update TargetFramework to net10.0 in SemanticClip.Client/SemanticClip.Client.csproj
- [ ] T011 Build solution (run `dotnet build SemanticClip.sln`) - GATE: must succeed
- [ ] T012 Run API independently (run `cd SemanticClip.API && dotnet run`) - GATE: must start
- [ ] T013 Run Client independently (run `cd SemanticClip.Client && dotnet run`) - GATE: must start
- [ ] T014 Test video processing workflow - GATE: must complete successfully
- [ ] T015 Commit Phase 2 changes (message: "Upgrade to .NET 10 runtime")

**Checkpoint**: .NET 10 runtime verified - Aspire integration can now begin

---

## Phase 3: User Story 1 - .NET 10 Runtime Migration (Priority: P1) 🎯 MVP

**Goal**: All projects running on .NET 10 with existing functionality preserved

**Independent Test**: Run `dotnet build` and execute video processing workflow end-to-end

### Validation for User Story 1

- [ ] T016 [US1] Verify all projects target net10.0 (grep TargetFramework across all .csproj files)
- [ ] T017 [US1] Verify global.json specifies 10.0.100 SDK
- [ ] T018 [US1] Run full build verification (dotnet build with zero errors)
- [ ] T019 [US1] Execute acceptance test: Upload test video, verify transcription completes
- [ ] T020 [US1] Execute acceptance test: Verify blog generation completes successfully
- [ ] T021 [US1] Document User Story 1 completion in CHANGELOG.md

**Checkpoint**: User Story 1 complete - .NET 10 fully operational

---

## Phase 4: User Story 2 - Aspire Orchestration Setup (Priority: P2)

**Goal**: Aspire Dashboard running with API and Client services visible

**Independent Test**: Run `dotnet run --project SemanticClip.AppHost` and verify Dashboard shows both services healthy

### Implementation for User Story 2

- [ ] T022 [P] [US2] Create SemanticClip.AppHost project directory
- [ ] T023 [US2] Create SemanticClip.AppHost/SemanticClip.AppHost.csproj with Aspire.Hosting.AppHost 13.0.2 package reference
- [ ] T024 [US2] Create SemanticClip.AppHost/Program.cs with DistributedApplication builder
- [ ] T025 [US2] Add API project reference to AppHost in SemanticClip.AppHost/Program.cs (builder.AddProject<Projects.SemanticClip_API>("api"))
- [ ] T026 [US2] Add Client project reference to AppHost in SemanticClip.AppHost/Program.cs (builder.AddProject<Projects.SemanticClip_Client>("client").WithReference(api))
- [ ] T027 [US2] Create SemanticClip.AppHost/appsettings.json with Dashboard configuration (optional auth: Dashboard.EnableAuth, Dashboard.User, Dashboard.Password - see quickstart.md for details)
- [ ] T028 [P] [US2] Create SemanticClip.ServiceDefaults project directory
- [ ] T029 [US2] Create SemanticClip.ServiceDefaults/SemanticClip.ServiceDefaults.csproj with Aspire.Hosting.ServiceDefaults 13.0.2 package reference
- [ ] T030 [US2] Create SemanticClip.ServiceDefaults/Extensions.cs with AddServiceDefaults method
- [ ] T031 [US2] Implement ConfigureOpenTelemetry in SemanticClip.ServiceDefaults/Extensions.cs
- [ ] T032 [US2] Implement AddDefaultHealthChecks in SemanticClip.ServiceDefaults/Extensions.cs (builder.Services.AddHealthChecks())
- [ ] T032.5 [US2] Add health check endpoints for API and Client in respective Program.cs files (app.MapHealthChecks("/health"))
- [ ] T033 [US2] Add AppHost project to SemanticClip.sln
- [ ] T034 [US2] Add ServiceDefaults project to SemanticClip.sln
- [ ] T035 [US2] Build solution (run `dotnet build SemanticClip.sln`) - GATE: must succeed
- [ ] T036 [US2] Run AppHost (run `dotnet run --project SemanticClip.AppHost`) - GATE: Dashboard must open
- [ ] T037 [US2] Verify Dashboard accessible at http://localhost:15888
- [ ] T038 [US2] Verify "Resources" tab shows api and client services with Running status
- [ ] T039 [US2] Commit Phase 4 changes (message: "Add Aspire orchestration with AppHost and ServiceDefaults")

**Checkpoint**: Aspire orchestration operational - services discoverable via Dashboard

---

## Phase 5: User Story 3 - Service Discovery Integration (Priority: P3)

**Goal**: Client discovers API endpoint dynamically without hardcoded URLs

**Independent Test**: Change API port in AppHost, restart services, verify Client automatically connects to new port

### Implementation for User Story 3

- [ ] T040 [US3] Add ServiceDefaults project reference to SemanticClip.API/SemanticClip.API.csproj
- [ ] T041 [US3] Add ServiceDefaults project reference to SemanticClip.Client/SemanticClip.Client.csproj
- [ ] T042 [US3] Add builder.AddServiceDefaults() call in SemanticClip.API/Program.cs (after builder creation)
- [ ] T043 [US3] Add builder.AddServiceDefaults() call in SemanticClip.Client/Program.cs (after builder creation)
- [ ] T044 [US3] Register service discovery in SemanticClip.Client/Program.cs (builder.Services.AddServiceDiscovery())
- [ ] T045 [US3] Update VideoProcessingApiClient to use service discovery in SemanticClip.Client/Services/VideoProcessingApiClient.cs (HttpClient BaseAddress = "http://api")
- [ ] T046 [US3] Update ExportApiClient to use service discovery in SemanticClip.Client/Services/ExportApiClient.cs (HttpClient BaseAddress = "http://api")
- [ ] T047 [US3] Remove hardcoded API URLs from SemanticClip.Client/wwwroot/appsettings.json (keep as commented fallback with note: "Used only when running Client independently outside Aspire orchestration")
- [ ] T048 [US3] Build solution (run `dotnet build SemanticClip.sln`) - GATE: must succeed
- [ ] T049 [US3] Run AppHost (run `dotnet run --project SemanticClip.AppHost`)
- [ ] T050 [US3] Execute acceptance test: Upload video via Client, verify API receives request (check Dashboard traces)
- [ ] T051 [US3] Execute acceptance test: Change API port in AppHost, restart, verify Client still connects
- [ ] T052 [US3] Commit Phase 5 changes (message: "Integrate service discovery for Client-to-API communication")

**Checkpoint**: Service discovery functional - Client dynamically discovers API

---

## Phase 6: User Story 4 - OpenTelemetry & Observability (Priority: P4)

**Goal**: Full distributed tracing visible in Aspire Dashboard for video processing workflow

**Independent Test**: Process a video and view complete trace in Dashboard showing all steps (upload → FFmpeg → transcription → blog generation)

**Note**: Verify actual service class names in SemanticClip.Services/Services/ before starting (current: VideoProcessingService.cs, AzureOpenAIAudioService.cs, AzureAIAgentService.cs, BlogPublishingService.cs)

### Implementation for User Story 4

- [ ] T053 [P] [US4] Add OpenTelemetry instrumentation for ASP.NET Core in SemanticClip.ServiceDefaults/Extensions.cs (WithMetrics and WithTracing)
- [ ] T054 [P] [US4] Add OpenTelemetry instrumentation for HttpClient in SemanticClip.ServiceDefaults/Extensions.cs
- [ ] T055 [P] [US4] Configure OTLP exporter in SemanticClip.ServiceDefaults/Extensions.cs
- [ ] T056 [US4] Create ActivitySource for FFmpeg operations in SemanticClip.Services/Services/VideoProcessingService.cs (new ActivitySource("SemanticClip.FFmpeg"))
- [ ] T057 [US4] Wrap FFmpeg audio extraction in Activity in SemanticClip.Services/Services/VideoProcessingService.cs (StartActivity("FFmpeg.ExtractAudio"))
- [ ] T058 [US4] Add FFmpeg exit code as Activity tag in SemanticClip.Services/Services/VideoProcessingService.cs (activity.SetTag("ffmpeg.exit_code", process.ExitCode))
- [ ] T059 [US4] Register FFmpeg ActivitySource in SemanticClip.ServiceDefaults/Extensions.cs (AddSource("SemanticClip.FFmpeg"))
- [ ] T060 [P] [US4] Add ActivitySource for transcription operations in SemanticClip.Services/Services/TranscriptionService.cs (new ActivitySource("SemanticClip.Transcription"))
- [ ] T061 [US4] Wrap Azure OpenAI Whisper call in Activity in SemanticClip.Services/Services/TranscriptionService.cs (StartActivity("Transcription.Process"))
- [ ] T062 [US4] Register Transcription ActivitySource in SemanticClip.ServiceDefaults/Extensions.cs (AddSource("SemanticClip.Transcription"))
- [ ] T063 [P] [US4] Add ActivitySource for blog generation in SemanticClip.Services/Services/BlogGenerationService.cs (new ActivitySource("SemanticClip.BlogGeneration"))
- [ ] T064 [US4] Wrap blog content generation in Activity in SemanticClip.Services/Services/BlogGenerationService.cs (StartActivity("BlogGeneration.Generate"))
- [ ] T065 [US4] Register BlogGeneration ActivitySource in SemanticClip.ServiceDefaults/Extensions.cs (AddSource("SemanticClip.BlogGeneration"))
- [ ] T066 [US4] Add wildcard ActivitySource registration in SemanticClip.ServiceDefaults/Extensions.cs (AddSource("SemanticClip.*"))
- [ ] T067 [US4] Build solution (run `dotnet build SemanticClip.sln`) - GATE: must succeed
- [ ] T068 [US4] Run AppHost (run `dotnet run --project SemanticClip.AppHost`)
- [ ] T069 [US4] Execute acceptance test: Upload video, monitor Dashboard "Traces" tab for distributed trace
- [ ] T070 [US4] Verify trace shows: Client request → API → FFmpeg activity → Transcription activity → BlogGeneration activity
- [ ] T071 [US4] Verify Dashboard "Structured Logs" shows FFmpeg command execution and exit codes
- [ ] T072 [US4] Execute acceptance test: Trigger error (e.g., invalid video format), verify error captured in trace with ActivityStatusCode.Error
- [ ] T073 [US4] Commit Phase 6 changes (message: "Add OpenTelemetry distributed tracing for AI workflow")

**Checkpoint**: Full observability operational - all workflow steps visible in Dashboard

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and validation

- [ ] T074 [P] Update CHANGELOG.md with migration details (User Stories 1-4 completed)
- [ ] T075 [P] Update README.md with new Aspire startup command (dotnet run --project SemanticClip.AppHost)
- [ ] T076 [P] Create migration documentation in docs/migration-to-aspire.md
- [ ] T077 [P] Update DEPLOYMENT.md with Aspire considerations (Dashboard not for production)
- [ ] T078 Validate quickstart.md instructions (run through all steps in specs/001-constitution-retrofit/quickstart.md)
- [ ] T079 Code cleanup: Remove commented hardcoded URLs from appsettings.json files
- [ ] T080 Code cleanup: Ensure consistent logging patterns across all services
- [ ] T081 Security review: Verify no secrets in appsettings.json or code
- [ ] T082 [P] Update constitution.md Technology Stack table to mark .NET 10 and Aspire as implemented
- [ ] T083 Final build verification (run `dotnet build SemanticClip.sln` with zero errors and warnings)
- [ ] T084 Final integration test: Complete video processing workflow via Aspire orchestration
- [ ] T085 Create Pull Request from 001-constitution-retrofit branch with summary of changes
- [ ] T086 Commit Phase 7 changes (message: "Documentation and final polish for Aspire migration")

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 - BLOCKS all user stories
- **Phase 3 (US1 Validation)**: Depends on Phase 2 - validates .NET 10 migration
- **Phase 4 (US2 Aspire Setup)**: Depends on Phase 3 - requires working .NET 10 projects
- **Phase 5 (US3 Service Discovery)**: Depends on Phase 4 - requires AppHost and ServiceDefaults
- **Phase 6 (US4 Observability)**: Depends on Phase 4 (ServiceDefaults) - can run parallel with Phase 5
- **Phase 7 (Polish)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Foundation for all others - MUST complete first
- **User Story 2 (P2)**: Requires US1 complete - creates Aspire infrastructure
- **User Story 3 (P3)**: Requires US2 complete - uses Aspire service discovery
- **User Story 4 (P4)**: Requires US2 complete - uses Aspire ServiceDefaults
  - US3 and US4 can run in parallel after US2

### Within Each User Story

- Phase 2: Target framework updates can all run in parallel [P]
- Phase 4: AppHost and ServiceDefaults creation can run in parallel [P] until integration
- Phase 6: ActivitySource creation in different services can run in parallel [P]

### Parallel Opportunities

- **Phase 2**: T006-T010 (all TargetFramework updates) can run in parallel
- **Phase 4**: T022-T027 (AppHost) and T028-T032 (ServiceDefaults) can run in parallel
- **Phase 6**: T053-T055 (ServiceDefaults OpenTelemetry), T056-T058 (FFmpeg), T060-T061 (Transcription), T063-T064 (BlogGeneration) can run in parallel
- **Phase 7**: T074-T077, T079-T082 (documentation tasks) can run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Launch all target framework updates in parallel:
Task T006: "Update TargetFramework to net10.0 in SemanticClip.Core/SemanticClip.Core.csproj"
Task T007: "Update TargetFramework to net10.0 in SemanticClip.Infrastructure/SemanticClip.Infrastructure.csproj"
Task T008: "Update TargetFramework to net10.0 in SemanticClip.Services/SemanticClip.Services.csproj"
Task T009: "Update TargetFramework to net10.0 in SemanticClip.API/SemanticClip.API.csproj"
Task T010: "Update TargetFramework to net10.0 in SemanticClip.Client/SemanticClip.Client.csproj"
```

## Parallel Example: Phase 6 (Observability)

```bash
# Launch ActivitySource creation in different services in parallel:
Task T056: "Create ActivitySource for FFmpeg in VideoProcessingService.cs"
Task T060: "Create ActivitySource for transcription in TranscriptionService.cs"
Task T063: "Create ActivitySource for blog generation in BlogGenerationService.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1-2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - .NET 10 runtime)
3. Complete Phase 3: User Story 1 (validate .NET 10)
4. Complete Phase 4: User Story 2 (Aspire orchestration)
5. **STOP and VALIDATE**: Test Aspire Dashboard shows services
6. This is a minimal viable migration - services run via Aspire

### Full Constitutional Compliance (All User Stories)

1. Complete Phases 1-4 (Setup → Foundational → US1 → US2)
2. Add Phase 5: User Story 3 (service discovery) - can run parallel with Phase 6
3. Add Phase 6: User Story 4 (observability) - can run parallel with Phase 5
4. Complete Phase 7: Polish (documentation and cleanup)
5. Each story adds constitutional compliance without breaking previous work

### Parallel Team Strategy

With multiple developers:

1. Team completes Phases 1-4 together (Setup, Foundational, US1, US2)
2. Once US2 (Aspire infrastructure) is done:
   - Developer A: User Story 3 (service discovery)
   - Developer B: User Story 4 (observability)
3. Stories complete independently and integrate seamlessly
4. Team completes Phase 7 (Polish) together

---

## Notes

- **[P]** tasks = different files, no dependencies, safe to parallelize
- **[Story]** label maps task to specific user story for traceability
- Each user story should be independently completable and verifiable
- Commit after each phase for granular rollback capability
- Stop at any checkpoint to validate story independently
- All existing functionality must work identically post-migration (FR-012)
- Use Aspire Dashboard at http://localhost:15888 for validation throughout
- Fallback: If Aspire issues arise, can still run projects individually with `dotnet run`

---

## Validation Checkpoints

### After Phase 2 (Foundational)
✅ `dotnet build SemanticClip.sln` succeeds  
✅ Video processing workflow completes on .NET 10  
✅ All 5 projects run independently

### After Phase 4 (US2 - Aspire Setup)
✅ `dotnet run --project SemanticClip.AppHost` launches Dashboard  
✅ Dashboard shows api and client services with green status  
✅ Services start within 10 seconds

### After Phase 5 (US3 - Service Discovery)
✅ Client → API requests succeed without hardcoded URLs  
✅ Changing API port in AppHost, Client automatically reconnects  
✅ Dashboard traces show Client → API HTTP calls

### After Phase 6 (US4 - Observability)
✅ Video processing workflow creates distributed trace in Dashboard  
✅ Trace shows all steps: Upload → FFmpeg → Transcription → Blog Generation  
✅ FFmpeg logs visible in Dashboard with exit codes  
✅ Errors captured with ActivityStatusCode.Error

### After Phase 7 (Polish)
✅ CHANGELOG.md updated  
✅ README.md has Aspire startup instructions  
✅ quickstart.md validated  
✅ Pull Request created

---

**Total Tasks**: 87  
**Parallelizable Tasks**: 21 (24%)  
**Estimated Completion**: 2-3 days for experienced .NET developer  
**MVP Scope**: Phases 1-4 (Tasks T001-T039) = Basic Aspire orchestration  
**Full Scope**: All phases (Tasks T001-T086) = Complete constitutional compliance
