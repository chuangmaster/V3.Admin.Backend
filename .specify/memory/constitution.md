<!--
Sync Impact Report:
Version change: 1.2.1 → 1.3.0 (Account Management API Specification Alignment)
Added sections: Account Management Context clarification
Modified principles: 
  - Principle IV: Enhanced with account management specific examples and business codes
  - Security Requirements: Added account-specific security considerations
Removed sections: None
Templates requiring updates:
  ✅ plan-template.md - Already aligned with three-layer architecture and constitution checks
  ✅ spec-template.md - Already aligned with user story prioritization and requirements
  ✅ tasks-template.md - Already aligned with user story-based task organization
  ⚠ Command files - No command directory found, skipping validation
Follow-up TODOs: None - all placeholders filled, constitution now explicitly aligned with account management API specification
-->

# V3.Admin.Backend Constitution

## Project Background

This backend project serves as the API foundation for v3-admin-frontend, providing comprehensive user account management functionality. The system delivers web API services for user authentication, authorization, role management, and permission control. Core capabilities include user lifecycle management, role-based access control (RBAC), and fine-grained permission assignments that enable secure, scalable administrative operations for frontend applications.

**Target Users**: System administrators, application users requiring authenticated access, and frontend applications consuming admin APIs.

**Core Domains**: User Management, Role Management, Permission Management, Authentication Services, Authorization Services.

## Core Principles

### I. Code Quality Excellence (NON-NEGOTIABLE)
Code MUST adhere to C# 13 best practices with comprehensive XML documentation for all public APIs. Every public method, class, and property requires Traditional Chinese comments explaining purpose and usage. Code MUST follow PascalCase for public members, camelCase for private fields, and prefix interfaces with "I". Nullable reference types are mandatory - declare variables non-nullable and use `is null`/`is not null` checks. Pattern matching and switch expressions MUST be used wherever applicable. File-scoped namespaces and single-line using directives are required.

**Rationale**: Maintains consistent, readable, and maintainable codebase that supports long-term evolution and team collaboration in an admin system requiring high reliability.

### II. Three-Layer Architecture Compliance
All features MUST implement the three-layer architecture: Presentation (Controllers + DTOs), Business Logic (Services), and Data Access (Repositories + Entities). Controllers MUST only handle HTTP concerns and delegate business logic to services. Services MUST use DTOs for data transfer and inject repositories via dependency injection. Repositories MUST work with entity models and handle all data persistence concerns. All dependencies MUST be registered in Program.cs using dependency injection.

**Rationale**: Ensures separation of concerns, testability, and maintainable architecture that scales with user management complexity and role/permission requirements.

### III. Test-First Development (NON-NEGOTIABLE)
Tests MUST be written before implementation. Critical paths MUST have unit tests with clear naming conventions matching existing style. Integration tests are required for API endpoints, authentication flows, role assignments, permission validations, and cross-layer interactions. Test coverage MUST be maintained for business logic layers. Tests MUST be independently executable and not depend on external resources without proper mocking.

**Rationale**: Prevents regressions, ensures reliability, and documents expected behavior for future maintainers in security-critical user management operations.

### IV. User Experience Consistency & Admin Interface Standards
All API responses MUST use ApiResponseModel wrapper with consistent Success, Code, Message, Data, Timestamp, and TraceId properties. The dual-layer design combining HTTP status codes (reflecting request processing state) and business logic codes (providing fine-grained business scenarios) is MANDATORY. Error responses MUST follow standardized format with appropriate HTTP status codes AND meaningful business codes from ResponseCodes constants. Authentication flows MUST provide clear, actionable error messages in Traditional Chinese. API endpoints MUST implement proper validation with meaningful error responses using appropriate business codes (e.g., VALIDATION_ERROR, INVALID_CREDENTIALS, USERNAME_EXISTS, PASSWORD_SAME_AS_OLD, CANNOT_DELETE_SELF, LAST_ACCOUNT_CANNOT_DELETE). Account management operations MUST provide detailed feedback on business rule violations (e.g., cannot delete last account, cannot delete current logged-in account) using specific business codes. Response times MUST be predictable and documented (<200ms for simple operations like login, <2000ms for complex operations like paginated list queries). Admin interface operations MUST maintain consistent patterns across all account management endpoints (login, create, update, delete, change password, list accounts). TraceId MUST be included in all responses for distributed tracing and troubleshooting.

**Rationale**: Provides predictable, reliable API behavior that enables consistent frontend integration, fine-grained error handling, and positive administrative user experience across all account management functions while supporting monitoring and debugging capabilities. Specific business codes for account operations enable frontend to provide contextual, user-friendly error messages.

### V. Performance & Security Standards for Account Management
API endpoints MUST respond within 200ms for simple operations (login, single account query) and 2000ms for complex operations (paginated account lists). Asynchronous programming patterns are mandatory for I/O operations. JWT authentication MUST be properly implemented with secure token generation, validation, and 1-hour expiration. All user inputs MUST be validated using FluentValidation with clear validation rules (username 3-20 chars alphanumeric+underscore, password min 8 chars, displayName 1-100 chars). Sensitive information (passwords, tokens) MUST NOT be logged or exposed in error messages - passwords MUST be hashed with BCrypt (work factor 12) before storage. Database queries MUST be optimized to prevent N+1 problems, especially for account list pagination. Concurrent updates MUST be handled with optimistic locking (RowVersion) to prevent data conflicts. Soft delete mechanism MUST be implemented for account deletion with business rules (cannot delete self, cannot delete last account). Rate limiting SHOULD be considered for authentication endpoints to prevent brute-force attacks.

**Rationale**: Ensures application remains responsive under administrative load while maintaining security standards appropriate for account management systems. BCrypt password hashing, JWT tokens, and validation rules align with industry best practices and the implemented API specification.

## API Response Design Standards

**Dual-Layer Response Model**: All endpoints MUST return ApiResponseModel<T> combining HTTP status codes with business logic codes. HTTP status codes reflect request processing state (2xx success, 4xx client errors, 5xx server errors). Business logic codes provide fine-grained scenario information using ResponseCodes constants.

**Success Responses**: Use ApiResponseModel<T>.CreateSuccess() with appropriate data, message, and business code (SUCCESS, CREATED, UPDATED, DELETED). HTTP status codes MUST match the operation (200 OK, 201 Created, etc.).

**Error Responses**: Use ApiResponseModel<T>.CreateFailure() with descriptive Traditional Chinese messages and specific business codes. Examples:
- 400 Bad Request + VALIDATION_ERROR for input validation failures (帳號長度必須為 3-20 字元)
- 401 Unauthorized + INVALID_CREDENTIALS for authentication failures (帳號或密碼錯誤)
- 401 Unauthorized + UNAUTHORIZED for missing/invalid JWT token (未授權,請先登入)
- 403 Forbidden + FORBIDDEN for authorization failures (您只能更新自己的資訊)
- 403 Forbidden + CANNOT_DELETE_SELF for account self-deletion attempts (無法刪除當前登入的帳號)
- 404 Not Found + NOT_FOUND for missing resources (帳號不存在)
- 409 Conflict + CONCURRENT_UPDATE_CONFLICT for optimistic locking failures (資料已被其他使用者更新,請重新載入後再試)
- 422 Unprocessable Entity + USERNAME_EXISTS for duplicate usernames (帳號已存在)
- 422 Unprocessable Entity + PASSWORD_SAME_AS_OLD for password change validation (新密碼不可與舊密碼相同)
- 422 Unprocessable Entity + LAST_ACCOUNT_CANNOT_DELETE for business rule violations (無法刪除最後一個有效帳號)
- 500 Internal Server Error + INTERNAL_ERROR for system failures (系統內部錯誤,請稍後再試)

**Required Fields**: All responses MUST include Success (bool), Code (string), Message (string), Timestamp (DateTime), and TraceId (string) for request tracking. Data field uses JsonIgnoreCondition.WhenWritingNull.

**Controller Helper Methods**: Controllers SHOULD implement helper methods (Success, Created, ValidationError, NotFound, Conflict, BusinessError, InternalError) that return IActionResult with properly configured ApiResponseModel and HTTP status codes.

**Frontend Integration**: Frontend applications can rely on both HTTP status codes (for API gateway/monitoring) and business codes (for fine-grained error handling and user experience customization).

## Security Requirements

**Authentication**: JWT Bearer token authentication is mandatory for protected endpoints. Tokens MUST expire within 1 hour (per API specification). Token generation MUST include user claims (UserId, Username, DisplayName). Refresh token mechanism SHOULD be implemented for future enhancements but is not required for current account management MVP.

**Authorization**: Currently implements single-tier account management without roles. All authenticated users have equal access to account management operations with self-service restrictions (users can only update their own profile and change their own password). Administrative functions (create account, delete account) require authentication. Future role-based authorization MAY be added but is out of scope for current implementation.

**Input Validation**: All user inputs MUST be validated using FluentValidation with explicit rules:
  - Username: 3-20 characters, alphanumeric + underscore only (^[a-zA-Z0-9_]+$)
  - Password: Minimum 8 characters, supports all Unicode characters
  - DisplayName: 1-100 characters
  - Confirmation: Exact match "CONFIRM" for delete operations
SQL injection prevention MUST be ensured through parameterized queries (Dapper). Special attention to username uniqueness validation.

**Data Protection**: Password storage MUST use BCrypt hashing with work factor 12. Audit logs MUST be maintained via CreatedAt/UpdatedAt timestamps and soft delete tracking (IsDeleted, DeletedAt). Concurrent updates MUST be protected via optimistic locking (RowVersion). Passwords MUST NEVER appear in logs, responses, or error messages.

**Error Handling**: Global exception handling middleware (ExceptionHandlingMiddleware) MUST be implemented. Sensitive information MUST NOT be exposed in production error responses. All errors MUST be logged with TraceId for correlation. Security-relevant events (failed login attempts, account deletions) SHOULD be logged at appropriate levels.

## Development Workflow

**Code Reviews**: All code changes MUST be reviewed by at least one team member. Reviews MUST verify compliance with all constitution principles. Performance implications MUST be considered for database queries and API design, especially for user lookup and permission validation operations.

**Dependency Management**: New dependencies MUST be justified and approved. Dependencies MUST be kept up-to-date with security patches. Package references MUST specify explicit version numbers. Special scrutiny for authentication/authorization libraries.

**Documentation**: All specifications, plans, and user-facing documentation MUST be written in Traditional Chinese (zh-TW). README files MUST be maintained with current setup instructions. API endpoints MUST be documented with OpenAPI/Swagger. Database schema changes MUST be documented with migration scripts. Role and permission models MUST be thoroughly documented.

**Environment Management**: Environment-specific configurations MUST be properly separated. Secrets MUST NOT be committed to source control. Development, staging, and production environments MUST be properly configured with appropriate user management security levels.

## Governance

This constitution supersedes all other development practices and MUST be followed for all code changes. Amendments require team consensus and proper documentation of changes. All pull requests MUST verify compliance with these principles before approval.

**Language Requirements**: Constitution and technical documentation MUST be written in English. All specifications, plans, user-facing documentation, error messages, and comments MUST be written in Traditional Chinese (zh-TW).

**Complexity Justification**: Any violation of these principles MUST be explicitly justified in code comments and pull request descriptions. Simpler alternatives MUST be documented and explained why they were rejected.

**Compliance Review**: Constitution compliance is verified during code reviews and MUST block merging of non-compliant code. Regular reviews of constitution effectiveness are required quarterly.

**Version**: 1.3.0 | **Ratified**: 2025-10-25 | **Last Amended**: 2025-10-30
