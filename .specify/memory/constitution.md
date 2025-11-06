<!--
Sync Impact Report:
Version change: 1.4.0 → 1.5.0 (Foreign Key Integrity & Permission Authorization Standards)
Added sections: 
  - Principle III: Database Design & Foreign Key Integrity (NON-NEGOTIABLE)
  - Principle IV: Permission-Based Authorization Design (NON-NEGOTIABLE)
Modified principles: 
  - Renumbered existing Principle III → V (Test-First Development)
  - Renumbered existing Principle IV → VI (User Experience Consistency & Admin Interface Standards)
  - Renumbered existing Principle V → VII (Performance & Security Standards for Account Management)
  - Principle I: Database Naming Standards remain unchanged (established in 1.4.0)
Removed sections: None
Templates requiring updates:
  ✅ plan-template.md - Aligned with foreign key requirements and permission design patterns
  ✅ spec-template.md - Aligned with database integrity and authorization requirements
  ✅ tasks-template.md - Aligned with migration tasks requiring FK constraints and permission seeding
  ⚠ Migration files - Require amendment migrations to add missing foreign key constraints (see Principle III for full list)
  ⚠ Future feature specs - MUST include permission definitions and [RequirePermission] attributes (see Principle IV)
Follow-up TODOs: 
  - Create amendment migration(s) to add missing foreign key constraints to existing tables (users.deleted_by, permissions.created_by/updated_by/deleted_by, roles.created_by/updated_by/deleted_by, role_permissions.assigned_by, user_roles.assigned_by/deleted_by, audit_logs.operator_id, permission_failure_logs.user_id)
  - Update developer documentation to highlight mandatory foreign key and permission design requirements
-->

# V3.Admin.Backend Constitution

## Project Background

This backend project serves as the API foundation for v3-admin-frontend, providing comprehensive user account management functionality. The system delivers web API services for user authentication, authorization, role management, and permission control. Core capabilities include user lifecycle management, role-based access control (RBAC), and fine-grained permission assignments that enable secure, scalable administrative operations for frontend applications.

**Target Users**: System administrators, application users requiring authenticated access, and frontend applications consuming admin APIs.

**Core Domains**: User Management, Role Management, Permission Management, Authentication Services, Authorization Services.

## Core Principles

### I. Code Quality Excellence (NON-NEGOTIABLE)
Code MUST adhere to C# 13 best practices with comprehensive XML documentation for all public APIs. Every public method, class, and property requires Traditional Chinese comments explaining purpose and usage. Code MUST follow PascalCase for public members, camelCase for private fields, and prefix interfaces with "I". Nullable reference types are mandatory - declare variables non-nullable and use `is null`/`is not null` checks. Pattern matching and switch expressions MUST be used wherever applicable. File-scoped namespaces and single-line using directives are required.

**Database Naming Standards**: Database objects MUST use snake_case naming convention while C# code uses PascalCase. This separation maintains PostgreSQL best practices while preserving C# conventions:
- **Tables**: snake_case plural nouns (e.g., `users`, `user_roles`, `permission_assignments`)
- **Columns**: snake_case (e.g., `id`, `username`, `password_hash`, `created_at`, `is_deleted`)
- **Indexes**: `idx_tablename_columnname` pattern (e.g., `idx_users_username`, `idx_users_createdat`)
- **Constraints**: `chk_tablename_description` for checks, `fk_tablename_column` for foreign keys (e.g., `chk_username_length`, `fk_users_deletedrby`)
- **C# Entities**: PascalCase properties that map to snake_case columns (e.g., `Id` → `id`, `CreatedAt` → `created_at`, `IsDeleted` → `is_deleted`)
- **ORM Mapping**: Use explicit column name mapping in Dapper queries or Entity Framework configurations to bridge naming conventions

**Rationale**: Maintains consistent, readable, and maintainable codebase that supports long-term evolution and team collaboration in an admin system requiring high reliability. Snake_case database naming follows PostgreSQL community standards and improves SQL readability, while PascalCase C# code follows .NET conventions.

### II. Three-Layer Architecture Compliance
All features MUST implement the three-layer architecture: Presentation (Controllers + DTOs), Business Logic (Services), and Data Access (Repositories + Entities). Controllers MUST only handle HTTP concerns and delegate business logic to services. Services MUST use DTOs for data transfer and inject repositories via dependency injection. Repositories MUST work with entity models and handle all data persistence concerns. All dependencies MUST be registered in Program.cs using dependency injection.

**Rationale**: Ensures separation of concerns, testability, and maintainable architecture that scales with user management complexity and role/permission requirements.

### III. Database Design & Foreign Key Integrity (NON-NEGOTIABLE)
All database tables MUST maintain referential integrity through properly defined foreign key constraints. Every column that references another table's primary key MUST have an explicit foreign key constraint. Foreign key constraints MUST specify appropriate `ON DELETE` and `ON UPDATE` behaviors:
- **Cascade deletion** (`ON DELETE CASCADE`): Use for strong ownership relationships (e.g., `role_permissions.role_id` → `roles.id`, `user_roles.user_id` → `users.id`) where child records should be automatically removed when parent is deleted.
- **Restrict deletion** (`ON DELETE RESTRICT` or `ON DELETE NO ACTION`): Use for audit/tracking columns (e.g., `users.deleted_by` → `users.id`, `permissions.created_by` → `users.id`) to prevent deletion of users who have performed operations in the system. These should use `ON DELETE SET NULL` if the relationship is optional.
- **Set null** (`ON DELETE SET NULL`): Use for optional references where the relationship can be broken without losing data integrity (e.g., audit columns like `created_by`, `updated_by`, `deleted_by`, `assigned_by`).

**Mandatory Foreign Keys** (examples from current schema):
- `users.deleted_by` → `users.id` (ON DELETE SET NULL)
- `permissions.created_by`, `permissions.updated_by`, `permissions.deleted_by` → `users.id` (ON DELETE SET NULL)
- `roles.created_by`, `roles.updated_by`, `roles.deleted_by` → `users.id` (ON DELETE SET NULL)
- `role_permissions.role_id` → `roles.id` (ON DELETE CASCADE)
- `role_permissions.permission_id` → `permissions.id` (ON DELETE CASCADE)
- `role_permissions.assigned_by` → `users.id` (ON DELETE SET NULL)
- `user_roles.user_id` → `users.id` (ON DELETE CASCADE)
- `user_roles.role_id` → `roles.id` (ON DELETE CASCADE)
- `user_roles.assigned_by`, `user_roles.deleted_by` → `users.id` (ON DELETE SET NULL)
- `audit_logs.operator_id` → `users.id` (ON DELETE SET NULL)
- `permission_failure_logs.user_id` → `users.id` (ON DELETE SET NULL)

**Migration Requirements**: All foreign key constraints MUST be added in the same migration file where the table is created or in a dedicated amendment migration. Constraint names MUST follow the pattern `fk_tablename_columnname` (e.g., `fk_users_deletedby`, `fk_permissions_createdby`).

**Rationale**: Foreign key constraints ensure data integrity, prevent orphaned records, maintain audit trail reliability, and make the database schema self-documenting. They catch referential integrity violations at the database level rather than application level, providing a critical defense against data corruption. Proper `ON DELETE` behaviors prevent cascade failures in audit systems while maintaining cleanup automation for transactional data.

### IV. Permission-Based Authorization Design (NON-NEGOTIABLE)
All new features MUST implement permission-based authorization following the established pattern. Every protected endpoint MUST be decorated with `[RequirePermission("resource.action")]` attribute where `resource` is the feature domain (e.g., `permission`, `role`, `account`, `inventory`) and `action` is the operation (e.g., `create`, `read`, `update`, `delete`, `assign`, `remove`). Permission codes MUST follow the dot notation format `resource.action` and be pre-defined in the `seed_permissions.sql` script before feature deployment.

**Required Permission Pattern**:
1. Define permissions in `Database/Scripts/seed_permissions.sql` using the format:
   ```sql
   INSERT INTO permissions (permission_code, name, description, permission_type) 
   VALUES 
       ('resource.read', '查詢[資源]', '允許查詢[資源]資訊', 'function'),
       ('resource.create', '新增[資源]', '允許創建新的[資源]', 'function'),
       ('resource.update', '修改[資源]', '允許編輯[資源]資訊', 'function'),
       ('resource.delete', '刪除[資源]', '允許刪除[資源]', 'function')
   ON CONFLICT (permission_code) DO NOTHING;
   ```

2. Apply `[RequirePermission]` attribute to controller endpoints:
   ```csharp
   [HttpGet]
   [RequirePermission("resource.read")]
   public async Task<IActionResult> GetResources() { ... }
   
   [HttpPost]
   [RequirePermission("resource.create")]
   public async Task<IActionResult> CreateResource() { ... }
   ```

3. Middleware `PermissionAuthorizationMiddleware` automatically validates permissions by:
   - Extracting user ID from JWT claims (`sub` claim)
   - Querying user's permissions through `IPermissionValidationService`
   - Returning 403 Forbidden if permission check fails
   - Logging permission failures to `permission_failure_logs` table

**Standard Permission Actions**: `create`, `read`, `update`, `delete` for CRUD operations; `assign`, `remove` for relationship management (e.g., role assignments); custom actions as needed (e.g., `export`, `import`, `approve`).

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

**Version**: 1.5.0 | **Ratified**: 2025-10-25 | **Last Amended**: 2025-11-07
