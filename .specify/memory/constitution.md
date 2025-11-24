<!--
Sync Impact Report:
Version change: 1.7.0 → 1.8.0 (MINOR - Added Paginated Response Principle and refined DTO rules)
Modified principles:
  - Principle VIII: Updated Response DTO naming convention to `xxxResponse` and clarified nested DTO mapping.
  - API Response Design Standards: Mandated the use of BaseController helper methods.
Added sections:
  - Principle IX: Paginated Response Design.
Removed sections: None
Templates requiring updates:
  ✅ plan-template.md - No changes needed, existing guidance is compatible.
  ✅ spec-template.md - No changes needed, existing guidance is compatible.
  ✅ tasks-template.md - No changes needed, existing guidance is compatible.
Follow-up TODOs:
  - Review all existing paginated endpoints to ensure they use PagedApiResponseModel<TItem>.
  - Review all controller endpoints to ensure they use BaseController helper methods for responses.
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

### VIII. Controller Response DTO Architecture (NON-NEGOTIABLE)
All Controller endpoints MUST implement a dedicated Response DTO layer that is separate from Service layer DTOs. Controllers MUST NOT directly return Service DTOs (e.g., `UserDto`, `PermissionDto`, `UserEffectivePermissionsDto`) as API responses. Instead, Controllers MUST create Response DTOs (named using `xxxResponse` pattern) that convert Service DTOs before returning to clients.

**Required Pattern**:
1. **Service Layer**: Returns business logic DTOs (e.g., `UserEffectivePermissionsDto` with `List<PermissionDto>`)
2. **Controller Layer**: Converts Service DTOs to Response DTOs (e.g., `UserEffectivePermissionsResponse` with `List<PermissionResponse>`)
3. **API Response**: Wraps Response DTO in `ApiResponseModel<T>` using `Success()` helper method from `BaseController`.

**Implementation Requirements**:
- Response DTOs MUST be placed in `Models/Responses/` directory.
- Response DTO naming MUST follow the `xxxResponse` pattern (e.g., `UserResponse`, `RoleDetailResponse`).
- Response DTOs MUST NOT reference Service DTO types in ANY way (not in properties, constructors, or methods).
- Response DTOs MUST NOT have constructors that accept Service DTO types as parameters.
- Conversion logic from Service DTO to Response DTO MUST be implemented in the Controller layer using explicit property mapping.
- Even if a Response DTO's structure is 1:1 identical to a Service DTO, the separation and independent mapping MUST be maintained.
- Nested objects within DTOs (e.g., a `List<PermissionDto>` inside `UserEffectivePermissionsDto`) MUST also have corresponding Response DTOs (e.g., `List<PermissionResponse>`) and be mapped explicitly.

**Example - Correct Pattern**:
```csharp
// Service Layer returns Service DTO
var serviceDto = await _service.GetUserEffectivePermissionsAsync(userId);

// Controller manually maps Service DTO to Response DTO (NO COUPLING)
var response = new UserEffectivePermissionsResponse
{
    UserId = serviceDto.UserId,
    Username = serviceDto.Username,
    Permissions = serviceDto.Permissions.Select(p => new PermissionResponse
    {
        PermissionId = p.PermissionId,
        PermissionCode = p.PermissionCode,
        Name = p.Name,
        Description = p.Description
    }).ToList()
};

// Wrap in ApiResponseModel and return using BaseController helper
return Success(response, "查詢成功");
```

**Example - Incorrect Patterns** (DO NOT USE):
```csharp
// ❌ WRONG: Directly returning Service DTO
var serviceDto = await _service.GetUserEffectivePermissionsAsync(userId);
return Success(serviceDto, "查詢成功");

// ❌ WRONG: Response DTO constructor depends on Service DTO type
public class UserEffectivePermissionsResponse
{
    // This creates coupling between Response DTO and Service DTO
    public UserEffectivePermissionsResponse(UserEffectivePermissionsDto serviceDto) { /* ... */ }
}
```

**Rationale**: Separating Controller Response DTOs from Service DTOs provides critical architectural benefits:
1. **Encapsulation**: Prevents internal business logic structures from leaking into public API contracts.
2. **Flexibility**: Enables independent evolution of API response formats without affecting business logic.
3. **Security**: Allows selective field exposure, hiding sensitive internal data.
4. **Versioning**: Facilitates API versioning by allowing multiple Response DTO versions to map to a single Service DTO.
5. **Frontend Stability**: Reduces frontend-backend coupling by providing a stable, purpose-built API contract.

This principle ensures long-term maintainability in a three-layer architecture where each layer has distinct responsibilities.

### IX. Paginated Response Design
For endpoints returning a paginated list of items, the response MUST use the `PagedApiResponseModel<TItem>` wrapper. This model flattens pagination properties (`PageNumber`, `PageSize`, `TotalCount`) to the top level alongside the `Items` collection, providing a clean and predictable structure for clients. Controllers MUST use the `CreatePagedSuccess` or `CreatePagedFailure` helper methods, preferably via a `BaseController`, to construct these responses.

**Rationale**: Standardizes the contract for all paginated data, simplifying frontend development of tables, lists, and infinite scrolling components. It ensures a consistent and easy-to-consume structure across the entire API.

## API Response Design Standards

**Dual-Layer Response Model**: All endpoints MUST return `ApiResponseModel<T>` (for single items) or `PagedApiResponseModel<T>` (for paginated lists), combining HTTP status codes with business logic codes. HTTP status codes reflect the request processing state (2xx success, 4xx client errors, 5xx server errors). Business logic codes from the `ResponseCodes` constants provide fine-grained scenario information.

**Response Creation**: Controllers MUST use the helper methods provided in a `BaseController` (e.g., `Success`, `Created`, `ValidationError`, `NotFound`, `BusinessError`, `InternalError`) to generate an `IActionResult` with a properly configured `ApiResponseModel` or `PagedApiResponseModel`.

**Success Responses**: Use helper methods that return `ApiResponseModel<T>.CreateSuccess()` or `ApiResponseFactory.CreatePagedSuccess<T>()`. The HTTP status code MUST match the operation (200 OK, 201 Created).

**Error Responses**: Use helper methods that return `ApiResponseModel<T>.CreateFailure()` or `ApiResponseFactory.CreatePagedFailure<T>()`. Responses MUST include descriptive Traditional Chinese messages and specific business codes. Examples:
- 400 Bad Request + `VALIDATION_ERROR`: for input validation failures (e.g., "帳號長度必須為 3-20 字元").
- 401 Unauthorized + `INVALID_CREDENTIALS`: for authentication failures (e.g., "帳號或密碼錯誤").
- 403 Forbidden + `FORBIDDEN`: for authorization failures (e.g., "您只能更新自己的資訊").
- 404 Not Found + `NOT_FOUND`: for missing resources (e.g., "帳號不存在").
- 409 Conflict + `CONCURRENT_UPDATE_CONFLICT`: for optimistic locking failures.
- 422 Unprocessable Entity + `USERNAME_EXISTS`: for duplicate usernames.
- 500 Internal Server Error + `INTERNAL_ERROR`: for system failures.

**Required Fields**: All responses MUST include `Success` (bool), `Code` (string), `Message` (string), `Timestamp` (DateTime), and `TraceId` (string) for request tracking.

## Security Requirements

**Authentication**: JWT Bearer token authentication is mandatory for protected endpoints. Tokens MUST expire within 1 hour. Token generation MUST include user claims (UserId, Username, DisplayName).

**Authorization**: Authorization is enforced via `[RequirePermission]` attributes. Self-service restrictions (users can only update/change their own data) must be enforced in the service layer.

**Input Validation**: All user inputs MUST be validated using FluentValidation. SQL injection MUST be prevented via parameterized queries (Dapper).

**Data Protection**: Passwords MUST be hashed with BCrypt (work factor 12). Sensitive data MUST NEVER appear in logs or responses. Concurrent updates MUST be protected via optimistic locking (RowVersion).

**Error Handling**: A global exception handling middleware MUST be implemented. It MUST log all errors with a `TraceId` and prevent sensitive information from being exposed in production responses.

## Development Workflow

**Code Reviews**: All code changes MUST be reviewed for compliance with all constitution principles.

**Dependency Management**: New dependencies MUST be justified and approved. Dependencies MUST be kept up-to-date.

**Documentation**: API endpoints MUST be documented with OpenAPI/Swagger. Database schema changes MUST be documented with migration scripts. All user-facing documentation, error messages, and comments MUST be in Traditional Chinese (zh-TW).

**Environment Management**: Environment-specific configurations MUST be properly separated. Secrets MUST NOT be committed to source control.

## Governance

This constitution supersedes all other development practices and MUST be followed for all code changes. Amendments require team consensus.

**Language Requirements**: The constitution and technical documentation are in English. All specifications, plans, user-facing documentation, error messages, and code comments are in Traditional Chinese (zh-TW).

**Compliance Review**: Constitution compliance is verified during code reviews and is mandatory for merging. The constitution's effectiveness is reviewed quarterly.

**Version**: 1.8.0 | **Ratified**: 2025-10-25 | **Last Amended**: 2025-11-25