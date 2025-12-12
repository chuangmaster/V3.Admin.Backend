<!--
Sync Impact Report:
Version change: 1.10.1 → 1.11.0 (MINOR - Separated technical implementation details from governance principles)
Modified principles:
  - All Principles (I-IX): Removed detailed code examples, SQL patterns, and implementation guides. Replaced with cross-references to technical-reference.md.
  - Principle I: Retained high-level code quality requirements. Moved naming conventions and database standards to Technical Reference §1.
  - Principle III: Retained foreign key integrity requirements. Moved ON DELETE/UPDATE behavior rules and complete foreign key list to Technical Reference §2.
  - Principle IV: Retained permission-based authorization design. Moved SQL seed scripts and RequirePermission examples to Technical Reference §3.
  - Principle VIII: Retained DTO separation requirements. Moved complete code examples and mapping patterns to Technical Reference §4.
  - Principle IX: Retained pagination architecture requirements. Moved 105-line implementation pattern to Technical Reference §5.
  - API Response Design Standards: Retained dual-layer response model concept. Moved HTTP status code mapping table to Technical Reference §6.
  - Security Requirements: Retained security standards. Moved specific parameters (BCrypt work factor, JWT expiration) to Technical Reference §7.
Added sections:
  - New file: technical-reference.md - Contains all extracted technical implementation details organized into 8 chapters.
Removed sections:
  - ~220 lines of code examples, SQL scripts, and implementation patterns moved to technical-reference.md.
Templates requiring updates:
  ✅ plan-template.md - Update to reference technical-reference.md for implementation details.
  ✅ spec-template.md - Update to reference technical-reference.md for code patterns.
  ✅ tasks-template.md - No changes needed, tasks reference principles which remain unchanged.
  ✅ agent-file-template.md - Update to include technical-reference.md as a required resource.
  ✅ checklist-template.md - Update validation checklists to reference technical-reference.md sections.
Follow-up TODOs:
  - Verify all cross-references between constitution.md and technical-reference.md are correct.
  - Update developer onboarding documentation to include technical-reference.md as a key resource.
  - Review technical-reference.md quarterly for accuracy and completeness.
  - Consider creating a quick-reference card summarizing constitution principles + technical-reference sections.
  - Constitution.md version updated to 1.11.0 with Last Amended date: 2025-12-13.
  - Created technical-reference.md version 1.0.0.
-->

# V3.Admin.Backend Constitution

## Project Background

This backend project serves as the API foundation for v3-admin-frontend, providing comprehensive user account management functionality. The system delivers web API services for user authentication, authorization, role management, and permission control. Core capabilities include user lifecycle management, role-based access control (RBAC), and fine-grained permission assignments that enable secure, scalable administrative operations for frontend applications.

**Target Users**: System administrators, application users requiring authenticated access, and frontend applications consuming admin APIs.

**Core Domains**: User Management, Role Management, Permission Management, Authentication Services, Authorization Services.

## Core Principles

### I. Code Quality Excellence (NON-NEGOTIABLE)
Code MUST adhere to C# 13 best practices with comprehensive XML documentation for all public APIs. Every public method, class, and property requires Traditional Chinese comments explaining purpose and usage. Code MUST follow established naming conventions, use nullable reference types with proper null checks, leverage pattern matching and switch expressions, and use file-scoped namespaces. Database objects MUST use snake_case naming convention while C# code uses PascalCase to maintain PostgreSQL best practices while preserving .NET conventions.

**Implementation Details**: See [Technical Reference §1: Code Style Guide](technical-reference.md#1-code-style-guide) for detailed naming conventions, nullable reference type patterns, modern C# features usage, and complete database-to-C# mapping examples.

**Rationale**: Maintains consistent, readable, and maintainable codebase that supports long-term evolution and team collaboration in an admin system requiring high reliability. Snake_case database naming follows PostgreSQL community standards and improves SQL readability, while PascalCase C# code follows .NET conventions.

### II. Three-Layer Architecture Compliance
All features MUST implement the three-layer architecture: Presentation (Controllers + DTOs), Business Logic (Services), and Data Access (Repositories + Entities). Controllers MUST only handle HTTP concerns and delegate business logic to services. Services MUST use DTOs for data transfer and inject repositories via dependency injection. Repositories MUST work with entity models and handle all data persistence concerns. All dependencies MUST be registered in Program.cs using dependency injection.

**Rationale**: Ensures separation of concerns, testability, and maintainable architecture that scales with user management complexity and role/permission requirements.

### III. Database Design & Foreign Key Integrity (NON-NEGOTIABLE)
All database tables MUST maintain referential integrity through properly defined foreign key constraints. Every column that references another table's primary key MUST have an explicit foreign key constraint with appropriate `ON DELETE` and `ON UPDATE` behaviors. Use CASCADE for strong ownership relationships, RESTRICT for required references, and SET NULL for optional audit/tracking columns. Constraint names MUST follow the `fk_tablename_columnname` pattern. All foreign key constraints MUST be added in the same migration file where the table is created or in a dedicated amendment migration.

**Implementation Details**: See [Technical Reference §2: Database Design Patterns](technical-reference.md#2-database-design-patterns) for the complete foreign key reference list (all 15+ constraints), ON DELETE/UPDATE behavior decision tree, and migration templates with examples.

**Rationale**: Foreign key constraints ensure data integrity, prevent orphaned records, maintain audit trail reliability, and make the database schema self-documenting. They catch referential integrity violations at the database level rather than application level, providing a critical defense against data corruption. Proper `ON DELETE` behaviors prevent cascade failures in audit systems while maintaining cleanup automation for transactional data.

### IV. Permission-Based Authorization Design (NON-NEGOTIABLE)
All new features MUST implement permission-based authorization following the established pattern. Every protected endpoint MUST be decorated with `[RequirePermission("resource.action")]` attribute where `resource` is the feature domain (e.g., `permission`, `role`, `account`, `inventory`) and `action` is the operation (e.g., `create`, `read`, `update`, `delete`, `assign`, `remove`). Permission codes MUST follow the dot notation format `resource.action` and be pre-defined in the `seed_permissions.sql` script before feature deployment. The `PermissionAuthorizationMiddleware` automatically validates permissions by extracting user ID from JWT claims, querying user permissions, returning 403 Forbidden on failures, and logging permission failures.

**Implementation Details**: See [Technical Reference §3: Authorization Implementation](technical-reference.md#3-authorization-implementation) for the complete permission setup workflow (seed script templates, RequirePermission attribute examples, middleware flow diagram), standard permission actions reference (CRUD + custom actions), and permission naming conventions.

**Rationale**: Standardized permission design ensures consistent security across features, enables fine-grained access control, supports role-based authorization (RBAC), facilitates audit compliance, and makes security requirements explicit in code through declarative attributes. The `resource.action` naming convention maintains clarity and prevents permission code collisions across different domains.

### V. Test-First Development (NON-NEGOTIABLE)
Tests MUST be written before implementation. Critical paths MUST have unit tests with clear naming conventions matching existing style. Integration tests are required for API endpoints, authentication flows, role assignments, permission validations, and cross-layer interactions. Test coverage MUST be maintained for business logic layers. Tests MUST be independently executable and not depend on external resources without proper mocking.

**Rationale**: Prevents regressions, ensures reliability, and documents expected behavior for future maintainers in security-critical user management operations.

### VI. User Experience Consistency & Admin Interface Standards
All API responses MUST use ApiResponseModel wrapper with consistent Success, Code, Message, Data, Timestamp, and TraceId properties. The dual-layer design combining HTTP status codes (reflecting request processing state) and business logic codes (providing fine-grained business scenarios) is MANDATORY. Error responses MUST follow standardized format with appropriate HTTP status codes AND meaningful business codes from ResponseCodes constants. Authentication flows MUST provide clear, actionable error messages in Traditional Chinese. API endpoints MUST implement proper validation with meaningful error responses using appropriate business codes (e.g., VALIDATION_ERROR, INVALID_CREDENTIALS, USERNAME_EXISTS, PASSWORD_SAME_AS_OLD, CANNOT_DELETE_SELF, LAST_ACCOUNT_CANNOT_DELETE). Account management operations MUST provide detailed feedback on business rule violations (e.g., cannot delete last account, cannot delete current logged-in account) using specific business codes. Response times MUST be predictable and documented (<200ms for simple operations like login, <2000ms for complex operations like paginated list queries). Admin interface operations MUST maintain consistent patterns across all account management endpoints (login, create, update, delete, change password, list accounts). TraceId MUST be included in all responses for distributed tracing and troubleshooting.

**Rationale**: Provides predictable, reliable API behavior that enables consistent frontend integration, fine-grained error handling, and positive administrative user experience across all account management functions while supporting monitoring and debugging capabilities. Specific business codes for account operations enable frontend to provide contextual, user-friendly error messages.

### VII. Performance & Security Standards for Account Management
API endpoints MUST respond within 200ms for simple operations (login, single account query) and 2000ms for complex operations (paginated account lists). Asynchronous programming patterns are mandatory for I/O operations. JWT authentication MUST be properly implemented with secure token generation, validation, and 1-hour expiration. All user inputs MUST be validated using FluentValidation with clear validation rules (username 3-20 chars alphanumeric+underscore, password min 8 chars, displayName 1-100 chars). Sensitive information (passwords, tokens) MUST NOT be logged or exposed in error messages - passwords MUST be hashed with BCrypt (work factor 12) before storage. Database queries MUST be optimized to prevent N+1 problems, especially for account list pagination. Concurrent updates MUST be handled with optimistic locking (RowVersion) to prevent data conflicts. Soft delete mechanism MUST be implemented for account deletion with business rules (cannot delete self, cannot delete last account). Rate limiting SHOULD be considered for authentication endpoints to prevent brute-force attacks.

**Rationale**: Ensures application remains responsive under administrative load while maintaining security standards appropriate for account management systems. BCrypt password hashing, JWT tokens, and validation rules align with industry best practices and the implemented API specification.

### VIII. Controller Response DTO Architecture (NON-NEGOTIABLE)
All Controller endpoints MUST implement a dedicated Response DTO layer that is separate from Service layer DTOs. Controllers MUST NOT directly return Service DTOs as API responses. Instead, Controllers MUST create Response DTOs (named using `xxxResponse` pattern, placed in `Models/Responses/`) that convert Service DTOs before returning to clients. Response DTOs MUST NOT reference Service DTO types in properties, constructors, or methods. Conversion logic MUST be implemented in the Controller layer using explicit property mapping. Even for 1:1 identical structures, the separation and independent mapping MUST be maintained. Nested objects within DTOs MUST also have corresponding Response DTOs and be mapped explicitly.

**Required Pattern**: Service Layer returns business logic DTOs → Controller Layer manually maps to Response DTOs → API Response wraps Response DTO in `ApiResponseModel<T>` using `BaseController` helper methods.

**Implementation Details**: See [Technical Reference §4: DTO Mapping Patterns](technical-reference.md#4-dto-mapping-patterns) for complete code examples demonstrating correct Service DTO → Response DTO mapping patterns, prohibited patterns (constructor coupling, direct Service DTO returns), and nested object mapping with LINQ transformations.

**Rationale**: Separating Controller Response DTOs from Service DTOs provides critical architectural benefits: (1) Encapsulation - prevents internal business logic structures from leaking into public API contracts, (2) Flexibility - enables independent evolution of API response formats without affecting business logic, (3) Security - allows selective field exposure, hiding sensitive internal data, (4) Versioning - facilitates API versioning by allowing multiple Response DTO versions to map to a single Service DTO, (5) Frontend Stability - reduces frontend-backend coupling by providing a stable, purpose-built API contract. This principle ensures long-term maintainability in a three-layer architecture where each layer has distinct responsibilities.

### IX. Pagination Architecture & Layer Responsibility (NON-NEGOTIABLE)
All paginated endpoints MUST implement strict layer separation to ensure database-level pagination execution, consistent response formats, and API layer testability. Repository layers MUST execute SQL queries with `LIMIT` and `OFFSET` clauses and provide separate COUNT queries. Service layers MUST return `PagedResultDto<TDto>` containing Items, TotalCount, PageNumber, and PageSize, executing pagination logic at the database level to prevent loading entire datasets into memory. Controller layers MUST validate pagination parameters, encapsulate query parameters into request objects, map Service DTOs to Response DTOs, and use `PagedApiResponseModel<TItem>` wrapper via `BaseApiController.PagedSuccess()` helper methods.

**Naming Conventions**: `PagedResultDto<T>` (Service layer return type in Models/Responses/), `PagedApiResponseModel<TItem>` (API layer wrapper in Models/ApiResponseModel.cs).

**Prohibited Patterns**: Service returning `PagedApiResponseModel<TItem>` (violates layer separation), Controller loading entire dataset into memory before pagination (violates performance requirements), Service accepting/returning API-specific models (violates encapsulation), Direct return of Service DTOs from Controllers without mapping to Response DTOs (violates Principle VIII).

**Implementation Details**: See [Technical Reference §5: Pagination Implementation Guide](technical-reference.md#5-pagination-implementation-guide) for the complete three-layer implementation pattern (Repository LIMIT/OFFSET queries, Service PagedResultDto construction, Controller validation and mapping), PagedResultDto<T> class definition, performance optimization strategies (COUNT query optimization, pagination index strategy), and common pitfalls to avoid.

**Rationale**: Enforces clear separation of concerns between business logic (Service) and API presentation (Controller). Database-level pagination prevents memory overflow on large datasets and ensures optimal query performance. Standardized `PagedResultDto<T>` enables consistent business logic testing without coupling to HTTP concerns. The `PagedApiResponseModel<TItem>` wrapper provides a clean, predictable API contract for frontend pagination components (tables, infinite scrolling). Mandatory DTO mapping maintains API contract independence from internal models, enabling independent evolution of business logic and API contracts. This pattern ensures pagination queries are safe, performant, testable, and maintainable across all features.

## API Response Design Standards

**Dual-Layer Response Model**: All endpoints MUST return `ApiResponseModel<T>` (for single items) or `PagedApiResponseModel<T>` (for paginated lists), combining HTTP status codes with business logic codes. HTTP status codes reflect the request processing state (2xx success, 4xx client errors, 5xx server errors). Business logic codes from the `ResponseCodes` constants provide fine-grained scenario information.

**Response Creation**: Controllers MUST use helper methods from `BaseController` (e.g., `Success`, `Created`, `ValidationError`, `NotFound`, `BusinessError`, `InternalError`) to generate properly configured responses. All responses MUST include `Success` (bool), `Code` (string), `Message` (string in Traditional Chinese), `Timestamp` (DateTime), and `TraceId` (string) for request tracking.

**Implementation Details**: See [Technical Reference §6: API Response Patterns](technical-reference.md#6-api-response-patterns) for the complete HTTP Status Code + Business Code mapping table (200-500 status codes with corresponding business codes and example messages) and BaseController helper methods with usage examples.

## Security Requirements

**Authentication**: JWT Bearer token authentication is mandatory for protected endpoints. Tokens MUST expire within 1 hour. Token generation MUST include user claims (UserId, Username, DisplayName).

**Authorization**: Authorization is enforced via `[RequirePermission]` attributes. Self-service restrictions (users can only update/change their own data) must be enforced in the service layer.

**Input Validation**: All user inputs MUST be validated using FluentValidation with clear validation rules. SQL injection MUST be prevented via parameterized queries (Dapper).

**Data Protection**: Passwords MUST be hashed with BCrypt using appropriate work factor. Sensitive data MUST NEVER appear in logs or responses. Concurrent updates MUST be protected via optimistic locking (RowVersion).

**Error Handling**: A global exception handling middleware MUST be implemented. It MUST log all errors with a `TraceId` and prevent sensitive information from being exposed in production responses.

**Implementation Details**: See [Technical Reference §7: Security Implementation](technical-reference.md#7-security-implementation) for JWT token generation/validation code examples, BCrypt password hashing patterns with work factor configuration, FluentValidation rule examples (username, password, displayName constraints), SQL injection prevention with Dapper parameterized queries, and sensitive data protection best practices (logging and error response guidelines).

## Development Workflow

**Code Reviews**: All code changes MUST be reviewed for compliance with all constitution principles.

**Dependency Management**: New dependencies MUST be justified and approved. Dependencies MUST be kept up-to-date.

**Documentation**: API endpoints MUST be documented with OpenAPI/Swagger. Database schema changes MUST be documented with migration scripts. All user-facing documentation, error messages, and comments MUST be in Traditional Chinese (zh-TW).

**Environment Management**: Environment-specific configurations MUST be properly separated. Secrets MUST NOT be committed to source control.

## Governance

This constitution supersedes all other development practices and MUST be followed for all code changes. Amendments require team consensus.

**Language Requirements**: 
- **Constitution and Core Documentation**: The constitution (this file) and foundational technical reference documents are written in English to maintain consistency and precision in governance terminology.
- **Feature Documentation (MANDATORY)**: All feature-specific documentation files MUST be written in **Traditional Chinese (zh-TW)**. This includes but is not limited to:
  - `plan.md` - Implementation plans
  - `tasks.md` - Task lists
  - `research.md` - Research documents
  - `quickstart.md` - Quickstart guides
  - `spec.md` - Feature specifications
  - `data-model.md` - Data model documentation
  - Any other feature-specific documentation in `/specs/` directories
- **Code and API**: All error messages, API responses, code comments, and user-facing strings MUST be in Traditional Chinese (zh-TW).
- **Rationale**: Traditional Chinese documentation ensures accessibility for the development team and stakeholders while maintaining technical precision through English governance documents.

**Compliance Review**: Constitution compliance is verified during code reviews and is mandatory for merging. The constitution's effectiveness is reviewed quarterly.

**Version**: 1.11.0 | **Ratified**: 2025-10-25 | **Last Amended**: 2025-12-13