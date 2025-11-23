---

description: "Task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/
**Language**: Tasks and descriptions MUST be written in Traditional Chinese (zh-TW) per constitution requirements

**Tests**: The examples below include test tasks. Tests are OPTIONAL - only include them if explicitly requested in the feature specification.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **C# ASP.NET Core Project**: `Controllers/`, `Services/`, `Repositories/`, `Models/` at repository root
- **Tests**: `Tests/Unit/`, `Tests/Integration/`, `Tests/Contract/` 
- **Interfaces**: `Services/Interfaces/`, `Repositories/Interfaces/`
- Paths shown below assume C# project structure - adjust based on plan.md structure

<!-- 
  ============================================================================
  IMPORTANT: The tasks below are SAMPLE TASKS for illustration purposes only.
  
  The /speckit.tasks command MUST replace these with actual tasks based on:
  - User stories from spec.md (with their priorities P1, P2, P3...)
  - Feature requirements from plan.md
  - Entities from data-model.md
  - Endpoints from contracts/
  
  Tasks MUST be organized by user story so each story can be:
  - Implemented independently
  - Tested independently
  - Delivered as an MVP increment
  
  DO NOT keep these sample tasks in the generated tasks.md file.
  ============================================================================
-->

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize [language] project with [framework] dependencies
- [ ] T003 [P] Configure linting and formatting tools

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete. All foundational tasks must follow constitution principles:

Examples of foundational tasks (adjust based on your project):

- [ ] T004 Setup Entity Framework Core or Dapper with database schema and migrations
- [ ] T005 [P] Implement JWT authentication middleware per constitution security requirements  
- [ ] T006 [P] Setup Controller base classes with ApiResponseModel wrapper and helper methods (Success, Created, ValidationError, NotFound, etc.)
- [ ] T007 Create base entity models and DTOs following three-layer architecture (User, Role, Permission entities)
- [ ] T008 Configure global exception handling middleware with Traditional Chinese error messages and ApiResponseModel responses
- [ ] T009 Setup environment configuration management with proper secrets handling
- [ ] T010 [P] Configure dependency injection for Services and Repositories in Program.cs
- [ ] T011 [P] Setup XML documentation generation and Swagger/OpenAPI documentation
- [ ] T012 [P] Implement role-based authorization framework and permission validation infrastructure
- [ ] T013 [P] Setup audit logging for user management operations per security requirements
- [ ] T014 [P] Implement TraceId middleware for distributed tracing and request tracking

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - [Title] (Priority: P1) 🎯 MVP

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 1 (OPTIONAL - only if tests requested) ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T010 [P] [US1] Contract test for [endpoint] in Tests/Contract/[ControllerName]ControllerTests.cs
- [ ] T011 [P] [US1] Integration test for [user journey] in Tests/Integration/[Feature]IntegrationTests.cs

### Implementation for User Story 1

- [ ] T012 [P] [US1] Create [Entity1] model in Models/Entities/[Entity1].cs following constitution naming conventions (PascalCase properties)
- [ ] T013 [P] [US1] Create [Entity2] model in Models/Entities/[Entity2].cs with XML documentation
- [ ] T014 [P] [US1] Create Service DTO in Models/Dtos/[Feature]Dto.cs for business logic layer data transfer
- [ ] T015 [P] [US1] Create Response DTO in Models/Responses/[Feature]ResponseDto.cs (MUST be separate from Service DTO per Principle VIII)
- [ ] T016 [US1] Implement I[Service] interface in Services/Interfaces/I[Service].cs
- [ ] T017 [US1] Implement [Service] in Services/[Service].cs with dependency injection, returning Service DTOs (depends on T012, T013, T014, T016)
- [ ] T018 [US1] Implement [endpoint/feature] in Controllers/[Controller].cs converting Service DTOs to Response DTOs and wrapping with ApiResponseModel using Success() helper
- [ ] T019 [US1] Add FluentValidation or data annotations for input validation
- [ ] T020 [US1] Add Traditional Chinese error handling with proper ResponseCodes and TraceId logging

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 2 (OPTIONAL - only if tests requested) ⚠️

- [ ] T019 [P] [US2] Contract test for [endpoint] in Tests/Contract/[ControllerName]ControllerTests.cs
- [ ] T020 [P] [US2] Integration test for [user journey] in Tests/Integration/[Feature]IntegrationTests.cs

### Implementation for User Story 2

- [ ] T021 [P] [US2] Create [Entity] model in Models/Entities/[Entity].cs with XML documentation
- [ ] T022 [P] [US2] Create Service DTO in Models/Dtos/[Feature]Dto.cs for business logic layer
- [ ] T023 [P] [US2] Create Response DTO in Models/Responses/[Feature]ResponseDto.cs (separate from Service DTO per Principle VIII)
- [ ] T024 [US2] Implement I[Service] interface in Services/Interfaces/I[Service].cs
- [ ] T025 [US2] Implement [Service] in Services/[Service].cs following three-layer architecture, returning Service DTOs
- [ ] T026 [US2] Implement [endpoint/feature] in Controllers/[Controller].cs converting Service DTOs to Response DTOs with ApiResponseModel and proper business codes
- [ ] T027 [US2] Integrate with User Story 1 components following dependency injection patterns

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - [Title] (Priority: P3)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 3 (OPTIONAL - only if tests requested) ⚠️

- [ ] T026 [P] [US3] Contract test for [endpoint] in Tests/Contract/[ControllerName]ControllerTests.cs
- [ ] T027 [P] [US3] Integration test for [user journey] in Tests/Integration/[Feature]IntegrationTests.cs

### Implementation for User Story 3

- [ ] T028 [P] [US3] Create [Entity] model in Models/Entities/[Entity].cs with proper nullable annotations
- [ ] T029 [P] [US3] Create Service DTO in Models/Dtos/[Feature]Dto.cs for internal business logic
- [ ] T030 [P] [US3] Create Response DTO in Models/Responses/[Feature]ResponseDto.cs (MUST NOT reference Service DTO types per Principle VIII)
- [ ] T031 [US3] Implement I[Service] interface in Services/Interfaces/I[Service].cs
- [ ] T032 [US3] Implement [Service] in Services/[Service].cs with async/await patterns, returning Service DTOs
- [ ] T033 [US3] Implement [endpoint/feature] in Controllers/[Controller].cs converting Service DTOs to Response DTOs with ApiResponseModel dual-layer design

**Checkpoint**: All user stories should now be independently functional

---

[Add more user story phases as needed, following the same pattern]

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] TXXX [P] Update XML documentation and README.md files
- [ ] TXXX Code cleanup and refactoring following C# 13 best practices
- [ ] TXXX Performance optimization with async/await patterns and database query optimization
- [ ] TXXX [P] Additional unit tests (if requested) in Tests/Unit/ following constitution test standards
- [ ] TXXX Security hardening - JWT token validation, input sanitization
- [ ] TXXX Constitution compliance verification across all implemented features
- [ ] TXXX Run quickstart.md validation and update Swagger documentation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - May integrate with US1/US2 but should be independently testable

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together (if tests requested):
Task: "Contract test for [endpoint] in Tests/Contract/[ControllerName]ControllerTests.cs"
Task: "Integration test for [user journey] in Tests/Integration/[Feature]IntegrationTests.cs"

# Launch all models for User Story 1 together:
Task: "Create [Entity1] model in Models/[Entity1].cs"
Task: "Create [Entity2] model in Models/[Entity2].cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
