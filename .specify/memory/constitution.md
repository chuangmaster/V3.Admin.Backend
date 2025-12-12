# V3.Admin.Backend Project Constitution

<!--
Sync Impact Report
================================================================================
Version Change: None → v1.0.0
Change Type: Initial Creation
Change Date: 2025-12-13

New Sections:
- Project Mission
- Five Core Principles:
  1. Simplicity First
  2. Test-Driven Quality
  3. Security Non-Negotiable
  4. Observability First
  5. Documentation as Code
- Governance Procedures (Amendment Process, Versioning Policy, Compliance Review)
- Project Scope (Tech Stack, Architecture Patterns, Core Features)
- Technical Debt Management
- Relationship with Technical Reference Documentation

Template Update Status:
✅ spec-template.md - Verified, aligns with constitution requirements (independently testable user stories, prioritization)
✅ plan-template.md - Updated
   - Added detailed constitution checklist (specific checkpoints for five principles)
   - Updated Technical Context to V3.Admin.Backend's concrete tech stack
   - Updated Project Structure to actual project structure (three-tier architecture, folder organization)
✅ tasks-template.md - Verified, aligns with constitution requirements (test-first, user story organization)
✅ technical-reference.md - Verified, complements constitution (HOW vs WHY/WHAT)

Related Documents Check:
✅ README.md - Verified, aligns with constitution principles (Traditional Chinese, security documentation, test coverage)
✅ copilot-instructions.md - Verified, aligns with constitution principles (three-tier architecture, testing, security)

Follow-up Actions:
- No pending items
- All templates aligned with constitution principles
- Recommendation: Use constitution checklist for validation in next feature development
- Recommendation: Conduct quarterly constitution compliance review

Version History:
v1.0.0 (2025-12-13) - Initial Creation
- Added five core principles (Simplicity First, Test-Driven, Security, Observability, Documentation as Code)
- Added governance procedures (amendment process, versioning policy, compliance review)
- Defined project scope (tech stack, architecture patterns, feature boundaries)
- Added technical debt management mechanisms (allowed types, forbidden types, repayment strategy)
- Integrated with technical reference documentation (WHY/WHAT vs HOW)
- Updated plan-template.md to include detailed constitution checklist
================================================================================
-->

**Constitution Version**: 1.0.0  
**Ratification Date**: 2025-12-13  
**Last Amended**: 2025-12-13  
**Project**: V3.Admin.Backend - Enterprise-Grade Admin System Backend API

---

## Project Mission

Build a modern, secure, high-performance enterprise-grade admin system API providing comprehensive authentication, permission management, and audit functionality. This project uses .NET 10 and PostgreSQL, follows three-tier architecture and industry best practices, delivering reliable backend services for the [V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite) frontend.

---

## Core Principles

### Principle 1: Simplicity First

**Definition**: Choose the simplest solution that solves the problem, avoiding over-engineering and unnecessary abstractions.

**Implementation Rules**:

- **MUST** Prioritize .NET built-in features, avoid introducing third-party packages unless clearly necessary
- **MUST** Adopt three-tier architecture (Controller → Service → Repository), no additional abstraction layers allowed
- **MUST** Use Dapper as data access technology, maintaining SQL transparency and control
- **MUST** Provide explicit rationale documentation for each new design pattern or architectural component
- **SHOULD** Code comments in Traditional Chinese, clearly explaining design decisions and business logic

**Rationale**: Simple systems are easier to understand, maintain, and debug. Over-engineering increases cognitive load, reduces development velocity, and raises long-term maintenance costs. In enterprise projects, code readability and maintainability take priority over demonstrating technical prowess.

**Checkpoints**:

- During new feature design reviews, must answer: "Is this the simplest way to solve this problem?"
- Pull Requests must include design decision explanations (if introducing new patterns or abstractions)
- During technical debt assessment, prioritize removal of unused abstractions and complexity

---

### Principle 2: Test-Driven Quality

**Definition**: Ensure code quality through comprehensive test coverage; every feature must be testable and validated through tests.

**Implementation Rules**:

- **MUST** All critical paths must have unit test coverage
- **MUST** All API endpoints must have integration tests, using Testcontainers to provide real database environment
- **MUST** Tests must be written before implementation (TDD), ensuring tests fail before implementation begins
- **MUST** Validators must achieve 100% test coverage (all validation rules and error messages)
- **MUST** Test files follow project naming conventions, do not use "Act", "Arrange", "Assert" comments
- **SHOULD** When delivering new features, all related tests must pass

**Rationale**: Testing is the cornerstone of quality assurance, preventing regressions, accelerating refactoring, and providing living documentation. Integration tests ensure system-level correctness, unit tests ensure component-level logic correctness.

**Checkpoints**:

- CI/CD pipeline must execute all tests, blocking merges when tests fail
- Code Review must check test coverage and test quality
- Quarterly review of test coverage reports, identifying uncovered critical paths

---

### Principle 3: Security Non-Negotiable

**Definition**: Security is core to system design, not a feature added afterward. All data processing, authentication, and authorization must follow industry standards.

**Implementation Rules**:

- **MUST** All API endpoints (except login) must pass JWT Bearer Token authentication
- **MUST** Passwords use BCrypt hashing (work factor 12), plain text storage forbidden
- **MUST** All database queries use parameterized queries (Dapper parameters), preventing SQL injection
- **MUST** Input validation uses FluentValidation, all request models must have corresponding validators
- **MUST** All delete operations adopt soft delete (marking `is_deleted`), preserving audit trail
- **MUST** Concurrent updates use optimistic locking (`version` field), preventing data races
- **MUST** Sensitive configuration (JWT SecretKey, database passwords) use environment variables or Azure Key Vault
- **MUST** All response error messages avoid leaking internal system information (use unified error format)

**Rationale**: Security vulnerabilities can lead to data breaches, system intrusion, and legal liability. Enterprise systems must build in security from the design phase, not rely on post-implementation patches.

**Checkpoints**:

- Each new feature must pass security checklist (authentication, authorization, input validation, data protection)
- Quarterly security audit, reviewing dependency vulnerabilities and system weaknesses
- Before penetration testing, must confirm all security rules are implemented

---

### Principle 4: Observability First

**Definition**: System must provide sufficient logging, tracing, and monitoring capabilities for rapid problem diagnosis and resolution.

**Implementation Rules**:

- **MUST** All HTTP requests must include `TraceId` (generated by `TraceIdMiddleware`)
- **MUST** All exceptions must log complete stack trace and context information
- **MUST** Critical operations (login, permission changes, data modifications) must write to audit log (`audit_logs` table)
- **MUST** Structured logging uses Serilog or built-in `ILogger`, including necessary context (user ID, operation type, timestamp)
- **SHOULD** API responses include unified format (`ApiResponseModel<T>`), containing `traceId` and `timestamp`
- **SHOULD** Production environment log level at `Information` or above, avoiding excessive logging affecting performance

**Rationale**: In distributed systems, observability is key to rapidly diagnosing problems, tracing request flow, and analyzing performance bottlenecks. Complete logging and tracing reduce Mean Time To Repair (MTTR).

**Checkpoints**:

- Code Review must check if critical paths have appropriate logging
- Each exception handling must log sufficient information to reproduce the problem
- Production environment issues must be traceable through TraceId for complete request chain

---

### Principle 5: Documentation as Code

**Definition**: Documentation is as important as code, must be updated synchronously with code, written in Traditional Chinese, and version controlled.

**Implementation Rules**:

- **MUST** All public APIs must have XML comment documentation (including `<summary>`, `<param>`, `<returns>`)
- **MUST** Each feature must have specification document (`spec.md`), implementation plan (`plan.md`), quickstart guide (`quickstart.md`)
- **MUST** Specification documents must include prioritized user stories (P1, P2, P3...), each story independently testable
- **MUST** API contract document (`api-spec.yaml`) must be synchronized with implementation, using OpenAPI 3.0 format
- **MUST** All user-facing documents (specs, guides, README) written in Traditional Chinese
- **MUST** Code comments in Traditional Chinese, clearly explaining business logic and design decisions
- **SHOULD** Swagger UI must provide complete API examples and descriptions

**Rationale**: Documentation is the foundation of knowledge transfer and team collaboration. Outdated or missing documentation leads to knowledge silos, increases onboarding time for new members, and raises maintenance costs. Traditional Chinese documentation reduces communication costs for local teams.

**Checkpoints**:

- Each Pull Request must include related documentation updates
- API changes must synchronously update OpenAPI specs and Swagger annotations
- Quarterly documentation review, confirming documentation-implementation consistency

---

## Governance

### Constitution Amendment Process

1. **Proposal**: Any team member can propose constitution amendments, must include:
   - Rationale for amendment and problem description
   - Amendment content (specific changes)
   - Impact assessment (effects on existing project and development processes)

2. **Discussion**: Proposal discussed in team meeting, collecting feedback and suggestions

3. **Approval**: Requires Tech Lead approval, major changes need team consensus

4. **Implementation**:
   - Update constitution version number (following semantic versioning)
   - Update `LAST_AMENDED_DATE`
   - Generate Sync Impact Report
   - Update related templates and documents (`spec-template.md`, `plan-template.md`, `tasks-template.md`)
   - Notify all team members

### Versioning Policy

Constitution version format: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes incompatible with previous versions (removing or redefining core principles)
  - Examples: Removing a core principle, changing architecture pattern requirements
- **MINOR**: Adding principles or significantly expanding existing guidance
  - Examples: Adding new core principle, expanding security requirements
- **PATCH**: Clarifications, wording adjustments, bug fixes
  - Examples: Fixing typos, improving clarity of descriptions, adding examples

### Compliance Review

- **Project Level**: Each new feature must pass "Constitution Check", confirming compliance with all core principles
- **Code Review**: All Pull Requests must check compliance with constitution requirements
- **Quarterly Review**: Conduct comprehensive review each quarter, identifying violations and improvement opportunities
- **Technical Debt Management**: If deviation from constitution principles is necessary, must be documented in `plan.md`'s "Complexity Tracking" section, including rationale and alternative solution evaluation

---

## Project Scope

### Tech Stack

- **Language**: C# 13
- **Framework**: ASP.NET Core 10.0
- **Database**: PostgreSQL 15+
- **ORM**: Dapper (Micro-ORM)
- **Authentication**: JWT Bearer Token
- **Password Hashing**: BCrypt.Net-Next
- **Input Validation**: FluentValidation
- **API Documentation**: Swagger/OpenAPI 3.0
- **Testing Framework**: xUnit, Moq, FluentAssertions, Testcontainers
- **Logging**: Serilog / Microsoft.Extensions.Logging

### Architecture Patterns

- **Three-Tier Architecture**: Controller → Service → Repository
- **DTO Pattern**: API layer and business layer data transfer objects separated
- **Repository Pattern**: Abstract data access logic
- **Middleware Pipeline**: Centralized exception handling, TraceId, permission validation
- **Unified Response Format**: `ApiResponseModel<T>` wrapping all API responses

### Core Feature Scope

1. **Authentication**: Login, JWT Token issuance and validation
2. **Account Management**: CRUD operations, password management, soft delete
3. **Permission Management**: Permission definition, assignment, validation
4. **Role Management**: Role CRUD, role-permission association
5. **Audit Logging**: Operation recording, querying, tracking

### Project Boundaries

**In Scope**:

- Backend API development (RESTful)
- Database design and migrations
- Unit testing and integration testing
- API documentation and technical documentation
- Docker containerization

**Out of Scope**:

- Frontend development (handled by V3 Admin Vite project)
- Email notification system (planned for future version)
- Two-factor authentication (planned for future version)
- File upload and storage (to be evaluated based on requirements)

---

## Technical Debt Management

### Debt Definition

Technical debt refers to non-optimal solutions adopted to accelerate delivery, increasing future maintenance costs.

### Allowed Debt Types

1. **Deliberate Debt**:
   - Must be documented in `plan.md`'s "Complexity Tracking" section
   - Needs rationale, impact scope, and repayment plan
   - Example: Temporarily using in-memory cache instead of Redis (planned for performance optimization phase)

2. **Temporary Debt**:
   - Used for rapid concept validation or prototyping
   - Must be repaid before official release
   - Example: Using hardcoded test data for Demo

### Forbidden Debt Types

1. **Security Debt**: Cannot omit security measures due to schedule pressure (violates Principle 3)
2. **Undocumented Debt**: Cannot deliver features without documentation (violates Principle 5)
3. **Untested Debt**: Cannot skip critical path testing (violates Principle 2)

### Debt Repayment

- Each Sprint must allocate time to repay technical debt (recommended 20% of time)
- Debt items must be tracked in backlog with priority marking
- Quarterly technical debt review, assessing accumulated risk

---

## Relationship with Technical Reference Documentation

This constitution defines "**WHY**" and "**WHAT**", while the [Technical Reference](technical-reference.md) defines "**HOW**".

- **Constitution**: Core principles, governance procedures, project scope, non-functional requirements
- **Technical Reference**: Code examples, naming conventions, database design patterns, implementation details

The two documents complement each other, forming the project's knowledge foundation. When technical implementation approaches change (e.g., using a different ORM), only the technical reference needs updating; when core principles change, the constitution needs revision.

---

## Appendices

### Related Documents

- [Technical Reference](technical-reference.md) - Implementation details and code examples
- [README.md](../../README.md) - Project overview and quick start
- [Spec Template](./../templates/spec-template.md) - Feature specification writing guide
- [Plan Template](./../templates/plan-template.md) - Implementation plan writing guide
- [Tasks Template](./../templates/tasks-template.md) - Task list organization guide

### Glossary

- **Three-Tier Architecture**: Controller (Presentation) → Service (Business Logic) → Repository (Data Access)
- **DTO** (Data Transfer Object): Data transfer objects for inter-layer data passing
- **Soft Delete**: Mark data as deleted rather than actually deleting, preserving audit trail
- **Optimistic Locking**: Use version numbers to prevent concurrent update conflicts
- **TraceId**: Request tracking identifier for correlating related logs in distributed systems

### Version History

| Version | Date | Change Summary |
|---------|------|----------------|
| 1.0.0 | 2025-12-13 | Initial creation - Defined five core principles, governance procedures, technical debt management |

---

**Document End**

*This constitution is a Living Document and should be continuously updated as the project evolves. All team members are responsible for following constitution principles and proposing improvements.*
