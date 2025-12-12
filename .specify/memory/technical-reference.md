# V3.Admin.Backend Technical Reference

**Version**: 1.0.0 | **Last Updated**: 2025-12-13

This document provides detailed technical implementation patterns, code examples, naming conventions, and best practices for the V3.Admin.Backend project. It complements the [Constitution](constitution.md) by providing the "HOW" details while the Constitution defines the "WHAT" and "WHY" governance principles.

---

## Table of Contents

1. [Code Style Guide](#1-code-style-guide)
2. [Database Design Patterns](#2-database-design-patterns)
3. [Authorization Implementation](#3-authorization-implementation)
4. [DTO Mapping Patterns](#4-dto-mapping-patterns)
5. [Pagination Implementation Guide](#5-pagination-implementation-guide)
6. [API Response Patterns](#6-api-response-patterns)
7. [Security Implementation](#7-security-implementation)
8. [Performance Best Practices](#8-performance-best-practices)

---

## 1. Code Style Guide

### 1.1 Naming Conventions

**C# Code Conventions**:
- **PascalCase**: Use for public members, method names, class names, and properties
  - Examples: `UserService`, `GetUserById()`, `DisplayName`
- **camelCase**: Use for private fields and local variables
  - Examples: `_userRepository`, `userId`, `passwordHash`
- **Interface Prefix**: Prefix all interface names with "I"
  - Examples: `IUserService`, `IPermissionRepository`

**Nullable Reference Types**:
- Declare variables as non-nullable by default
- Always use `is null` or `is not null` instead of `== null` or `!= null`
- Trust C# null annotations - avoid null checks when the type system guarantees non-null

**Modern C# Features**:
- Use **pattern matching** wherever applicable
- Prefer **switch expressions** over traditional switch statements
- Use **file-scoped namespace** declarations (C# 10+)
- Use **single-line using directives**

**Example**:
```csharp
// File-scoped namespace (C# 10+)
namespace V3.Admin.Backend.Services;

// Interface with "I" prefix
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken ct);
}

// Class with proper naming
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository; // camelCase for private field

    public async Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        
        // Use 'is null' instead of '== null'
        if (user is null)
            return null;

        // Pattern matching
        return user switch
        {
            { IsDeleted: true } => null,
            _ => MapToDto(user)
        };
    }
}
```

### 1.2 Database Naming Standards

**PostgreSQL Naming Convention** (snake_case):

| Object Type | Pattern | Examples |
|------------|---------|----------|
| **Tables** | `snake_case` plural nouns | `users`, `user_roles`, `permission_assignments`, `audit_logs` |
| **Columns** | `snake_case` | `id`, `username`, `password_hash`, `created_at`, `is_deleted`, `deleted_by` |
| **Indexes** | `idx_tablename_columnname` | `idx_users_username`, `idx_users_createdat`, `idx_permissions_permissioncode` |
| **Check Constraints** | `chk_tablename_description` | `chk_username_length`, `chk_password_length` |
| **Foreign Keys** | `fk_tablename_columnname` | `fk_users_deletedby`, `fk_permissions_createdby` |

**C# to Database Mapping**:

C# entity properties use PascalCase while database columns use snake_case. Ensure proper ORM mapping:

```csharp
// C# Entity (PascalCase properties)
public class User
{
    public int Id { get; set; }              // Maps to: id
    public string Username { get; set; }      // Maps to: username
    public string PasswordHash { get; set; }  // Maps to: password_hash
    public DateTime CreatedAt { get; set; }   // Maps to: created_at
    public bool IsDeleted { get; set; }       // Maps to: is_deleted
    public int? DeletedBy { get; set; }       // Maps to: deleted_by
}
```

**Dapper Mapping Example**:
```csharp
// Explicit column mapping in SQL queries
var sql = @"
    SELECT 
        id AS Id,
        username AS Username,
        password_hash AS PasswordHash,
        created_at AS CreatedAt,
        is_deleted AS IsDeleted,
        deleted_by AS DeletedBy
    FROM users 
    WHERE id = @UserId";

var user = await _connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
```

---

## 2. Database Design Patterns

### 2.1 Foreign Key Standards

All foreign key relationships MUST be explicitly defined with appropriate `ON DELETE` and `ON UPDATE` behaviors.

#### ON DELETE Behavior Decision Tree

```
┌─────────────────────────────────────┐
│ Does child record represent         │
│ ownership/strong dependency?        │
└──────────┬──────────────────────────┘
           │
    ┌──────┴───────┐
    │ YES          │ NO
    │              │
    ▼              ▼
CASCADE        ┌─────────────────────┐
               │ Is the reference    │
               │ optional/nullable?  │
               └──────┬──────────────┘
                      │
               ┌──────┴───────┐
               │ YES          │ NO
               │              │
               ▼              ▼
           SET NULL      RESTRICT
```

**Cascade Deletion** (`ON DELETE CASCADE`):
Use for strong ownership relationships where child records should be automatically removed when parent is deleted.

**Restrict Deletion** (`ON DELETE RESTRICT` or `ON DELETE NO ACTION`):
Use for references that must always be valid. Prevents deletion of parent if children exist.

**Set Null** (`ON DELETE SET NULL`):
Use for optional audit/tracking references where the relationship can be broken without losing data integrity.

#### Complete Foreign Key Reference

**Users Table Foreign Keys**:
```sql
ALTER TABLE users
    ADD CONSTRAINT fk_users_deletedby 
    FOREIGN KEY (deleted_by) REFERENCES users(id) 
    ON DELETE SET NULL;
```

**Permissions Table Foreign Keys**:
```sql
ALTER TABLE permissions
    ADD CONSTRAINT fk_permissions_createdby 
    FOREIGN KEY (created_by) REFERENCES users(id) 
    ON DELETE SET NULL;

ALTER TABLE permissions
    ADD CONSTRAINT fk_permissions_updatedby 
    FOREIGN KEY (updated_by) REFERENCES users(id) 
    ON DELETE SET NULL;

ALTER TABLE permissions
    ADD CONSTRAINT fk_permissions_deletedby 
    FOREIGN KEY (deleted_by) REFERENCES users(id) 
    ON DELETE SET NULL;
```

**Roles Table Foreign Keys**:
```sql
ALTER TABLE roles
    ADD CONSTRAINT fk_roles_createdby 
    FOREIGN KEY (created_by) REFERENCES users(id) 
    ON DELETE SET NULL;

ALTER TABLE roles
    ADD CONSTRAINT fk_roles_updatedby 
    FOREIGN KEY (updated_by) REFERENCES users(id) 
    ON DELETE SET NULL;

ALTER TABLE roles
    ADD CONSTRAINT fk_roles_deletedby 
    FOREIGN KEY (deleted_by) REFERENCES users(id) 
    ON DELETE SET NULL;
```

**Role Permissions Table Foreign Keys**:
```sql
ALTER TABLE role_permissions
    ADD CONSTRAINT fk_rolepermissions_roleid 
    FOREIGN KEY (role_id) REFERENCES roles(id) 
    ON DELETE CASCADE;

ALTER TABLE role_permissions
    ADD CONSTRAINT fk_rolepermissions_permissionid 
    FOREIGN KEY (permission_id) REFERENCES permissions(id) 
    ON DELETE CASCADE;

ALTER TABLE role_permissions
    ADD CONSTRAINT fk_rolepermissions_assignedby 
    FOREIGN KEY (assigned_by) REFERENCES users(id) 
    ON DELETE SET NULL;
```

**User Roles Table Foreign Keys**:
```sql
ALTER TABLE user_roles
    ADD CONSTRAINT fk_userroles_userid 
    FOREIGN KEY (user_id) REFERENCES users(id) 
    ON DELETE CASCADE;

ALTER TABLE user_roles
    ADD CONSTRAINT fk_userroles_roleid 
    FOREIGN KEY (role_id) REFERENCES roles(id) 
    ON DELETE CASCADE;

ALTER TABLE user_roles
    ADD CONSTRAINT fk_userroles_assignedby 
    FOREIGN KEY (assigned_by) REFERENCES users(id) 
    ON DELETE SET NULL;

ALTER TABLE user_roles
    ADD CONSTRAINT fk_userroles_deletedby 
    FOREIGN KEY (deleted_by) REFERENCES users(id) 
    ON DELETE SET NULL;
```

**Audit Logs Table Foreign Keys**:
```sql
ALTER TABLE audit_logs
    ADD CONSTRAINT fk_auditlogs_operatorid 
    FOREIGN KEY (operator_id) REFERENCES users(id) 
    ON DELETE SET NULL;
```

**Permission Failure Logs Table Foreign Keys**:
```sql
ALTER TABLE permission_failure_logs
    ADD CONSTRAINT fk_permissionfailurelogs_userid 
    FOREIGN KEY (user_id) REFERENCES users(id) 
    ON DELETE SET NULL;
```

### 2.2 Migration Requirements

- All foreign key constraints MUST be added in the same migration file where the table is created, or in a dedicated amendment migration
- Constraint names MUST follow the `fk_tablename_columnname` pattern
- Always specify explicit `ON DELETE` and `ON UPDATE` behavior (never rely on defaults)

**Migration Template**:
```sql
-- 003_CreateRolesTable.sql
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    role_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by INTEGER,
    updated_at TIMESTAMP,
    updated_by INTEGER,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    deleted_by INTEGER
);

-- Add foreign keys in the same migration
ALTER TABLE roles
    ADD CONSTRAINT fk_roles_createdby 
    FOREIGN KEY (created_by) REFERENCES users(id) 
    ON DELETE SET NULL;

ALTER TABLE roles
    ADD CONSTRAINT fk_roles_updatedby 
    FOREIGN KEY (updated_by) REFERENCES users(id) 
    ON DELETE SET NULL;

ALTER TABLE roles
    ADD CONSTRAINT fk_roles_deletedby 
    FOREIGN KEY (deleted_by) REFERENCES users(id) 
    ON DELETE SET NULL;

-- Add indexes
CREATE INDEX idx_roles_rolecode ON roles(role_code);
CREATE INDEX idx_roles_createdat ON roles(created_at);
```

---

## 3. Authorization Implementation

### 3.1 Permission System Setup

#### Step 1: Define Permissions in Seed Script

Create permissions in `Database/Scripts/seed_permissions.sql` using the `resource.action` format:

```sql
-- Example: Inventory Management Permissions
INSERT INTO permissions (permission_code, name, description, permission_type) 
VALUES 
    ('inventory.read', '查詢庫存', '允許查詢庫存資訊', 'function'),
    ('inventory.create', '新增庫存', '允許創建新的庫存項目', 'function'),
    ('inventory.update', '修改庫存', '允許編輯庫存資訊', 'function'),
    ('inventory.delete', '刪除庫存', '允許刪除庫存項目', 'function'),
    ('inventory.export', '匯出庫存', '允許匯出庫存報表', 'function')
ON CONFLICT (permission_code) DO NOTHING;
```

**Permission Naming Convention**:
- Format: `resource.action`
- Resource: Feature domain (singular form, e.g., `permission`, `role`, `account`, `inventory`)
- Action: Operation type (see Standard Actions below)

#### Step 2: Apply RequirePermission Attribute

Decorate controller endpoints with `[RequirePermission("resource.action")]`:

```csharp
using V3.Admin.Backend.Attributes;

namespace V3.Admin.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;

    [HttpGet]
    [RequirePermission("inventory.read")]
    public async Task<IActionResult> GetInventoryItems(
        [FromQuery] InventoryQueryRequest request, 
        CancellationToken ct)
    {
        // Implementation
    }

    [HttpPost]
    [RequirePermission("inventory.create")]
    public async Task<IActionResult> CreateInventoryItem(
        [FromBody] CreateInventoryRequest request, 
        CancellationToken ct)
    {
        // Implementation
    }

    [HttpPut("{id}")]
    [RequirePermission("inventory.update")]
    public async Task<IActionResult> UpdateInventoryItem(
        int id, 
        [FromBody] UpdateInventoryRequest request, 
        CancellationToken ct)
    {
        // Implementation
    }

    [HttpDelete("{id}")]
    [RequirePermission("inventory.delete")]
    public async Task<IActionResult> DeleteInventoryItem(
        int id, 
        CancellationToken ct)
    {
        // Implementation
    }

    [HttpGet("export")]
    [RequirePermission("inventory.export")]
    public async Task<IActionResult> ExportInventory(
        [FromQuery] InventoryExportRequest request, 
        CancellationToken ct)
    {
        // Implementation
    }
}
```

#### Step 3: Middleware Automatic Validation

The `PermissionAuthorizationMiddleware` automatically validates permissions:

1. **Extract User ID** from JWT claims (`sub` claim)
2. **Query User Permissions** through `IPermissionValidationService`
3. **Return 403 Forbidden** if permission check fails
4. **Log Permission Failures** to `permission_failure_logs` table

**Middleware Flow**:
```
HTTP Request
    │
    ├─→ JWT Authentication
    │       │
    │       ├─→ Extract UserId from 'sub' claim
    │       │
    │       ├─→ Check [RequirePermission] attribute
    │       │
    │       ├─→ Query User Effective Permissions
    │       │       (via IPermissionValidationService)
    │       │
    │       ├─→ Permission Found?
    │       │       │
    │       │   ┌───┴────┐
    │       │   │ YES    │ NO
    │       │   │        │
    │       │   ▼        ▼
    │       │ Allow   403 Forbidden
    │       │         + Log Failure
    │       │
    │       └─→ Continue to Controller
    │
    └─→ Controller Action Execution
```

### 3.2 Standard Permission Actions

**CRUD Operations**:
- `create` - Creating new resources
- `read` - Querying/viewing resources
- `update` - Modifying existing resources
- `delete` - Removing resources (soft or hard delete)

**Relationship Management**:
- `assign` - Assigning relationships (e.g., role assignments, permission assignments)
- `remove` - Removing relationships

**Custom Actions** (feature-specific):
- `export` - Exporting data/reports
- `import` - Importing data
- `approve` - Approval workflows
- `reject` - Rejection workflows
- `publish` - Publishing content
- `archive` - Archiving records

**Examples**:
```
permission.create     → Create new permission
permission.read       → Query permissions
role.assign          → Assign permissions to roles
account.delete       → Delete user accounts
inventory.export     → Export inventory reports
document.approve     → Approve documents
```

---

## 4. DTO Mapping Patterns

### 4.1 Service DTO ↔ Response DTO Separation

Controllers MUST maintain strict separation between Service DTOs (business logic layer) and Response DTOs (API presentation layer).

**Architecture Flow**:
```
Service Layer          Controller Layer         API Response
─────────────         ────────────────         ────────────
  Service DTO    →    Manual Mapping    →    Response DTO
     (e.g.,              (Controller)           (wrapped in
PermissionDto)                              ApiResponseModel)
```

**Prohibited Pattern** ❌:
```csharp
// WRONG: Response DTO constructor depends on Service DTO type
public class UserEffectivePermissionsResponse
{
    // This creates coupling between Response DTO and Service DTO
    public UserEffectivePermissionsResponse(UserEffectivePermissionsDto serviceDto)
    {
        UserId = serviceDto.UserId;
        Username = serviceDto.Username;
        Permissions = serviceDto.Permissions.Select(p => new PermissionResponse(p)).ToList();
    }
}
```

**Correct Pattern** ✅:
```csharp
// Step 1: Service Layer returns Service DTO
public async Task<UserEffectivePermissionsDto> GetUserEffectivePermissionsAsync(
    int userId, 
    CancellationToken ct)
{
    // Business logic implementation
    return new UserEffectivePermissionsDto
    {
        UserId = user.Id,
        Username = user.Username,
        Permissions = permissions.Select(p => new PermissionDto
        {
            PermissionId = p.PermissionId,
            PermissionCode = p.PermissionCode,
            Name = p.Name,
            Description = p.Description
        }).ToList()
    };
}

// Step 2: Controller manually maps Service DTO to Response DTO (NO COUPLING)
[HttpGet("{userId}/permissions")]
[RequirePermission("user.read")]
public async Task<IActionResult> GetUserEffectivePermissions(
    int userId, 
    CancellationToken ct)
{
    // Call service (returns Service DTO)
    var serviceDto = await _userService.GetUserEffectivePermissionsAsync(userId, ct);

    // Manual mapping to Response DTO (no constructor dependency)
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
}
```

### 4.2 Nested Object Mapping

When Service DTOs contain nested collections (e.g., `List<PermissionDto>`), Response DTOs MUST also have corresponding nested Response types (e.g., `List<PermissionResponse>`).

**Complete Example**:

```csharp
// Service Layer DTOs (in Models/Dtos/)
public class UserEffectivePermissionsDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class PermissionDto
{
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// API Layer Response DTOs (in Models/Responses/)
public class UserEffectivePermissionsResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<PermissionResponse> Permissions { get; set; } = new(); // NOT List<PermissionDto>
}

public class PermissionResponse
{
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Controller mapping (explicit property mapping for nested objects)
var response = new UserEffectivePermissionsResponse
{
    UserId = serviceDto.UserId,
    Username = serviceDto.Username,
    Permissions = serviceDto.Permissions.Select(dto => new PermissionResponse
    {
        PermissionId = dto.PermissionId,
        PermissionCode = dto.PermissionCode,
        Name = dto.Name,
        Description = dto.Description
    }).ToList()
};
```

**Key Rules**:
1. Response DTO properties MUST NOT reference Service DTO types
2. Mapping logic MUST be implemented in Controller layer using explicit property assignment
3. Even for 1:1 identical structures, maintain separate types for future flexibility
4. Use LINQ `.Select()` for collection transformations

---

## 5. Pagination Implementation Guide

### 5.1 Complete Implementation Pattern

Pagination MUST be executed at the database level using `LIMIT` and `OFFSET` to prevent loading entire datasets into memory.

#### Layer 1: Repository (Database-Level Pagination)

```csharp
// Repository Interface
public interface IPermissionRepository
{
    Task<IEnumerable<Permission>> GetPagedPermissionsAsync(
        int pageNumber, 
        int pageSize, 
        PermissionFilters filters, 
        CancellationToken ct);
    
    Task<long> CountPermissionsAsync(
        PermissionFilters filters, 
        CancellationToken ct);
}

// Repository Implementation
public class PermissionRepository : IPermissionRepository
{
    private readonly IDbConnection _connection;

    public async Task<IEnumerable<Permission>> GetPagedPermissionsAsync(
        int pageNumber, 
        int pageSize, 
        PermissionFilters filters, 
        CancellationToken ct)
    {
        var offset = (pageNumber - 1) * pageSize;
        
        var sql = @"
            SELECT 
                permission_id AS PermissionId,
                permission_code AS PermissionCode,
                name AS Name,
                description AS Description,
                permission_type AS PermissionType,
                created_at AS CreatedAt
            FROM permissions 
            WHERE is_deleted = false
            ORDER BY created_at DESC 
            LIMIT @PageSize OFFSET @Offset";
        
        return await _connection.QueryAsync<Permission>(
            new CommandDefinition(
                sql, 
                new { PageSize = pageSize, Offset = offset }, 
                cancellationToken: ct)
        );
    }

    public async Task<long> CountPermissionsAsync(
        PermissionFilters filters, 
        CancellationToken ct)
    {
        var sql = "SELECT COUNT(*) FROM permissions WHERE is_deleted = false";
        
        return await _connection.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, cancellationToken: ct)
        );
    }
}
```

#### Layer 2: Service (Returns PagedResultDto)

```csharp
// Service Interface
public interface IPermissionService
{
    Task<PagedResultDto<PermissionDto>> GetPermissionsAsync(
        PermissionQuery query, 
        CancellationToken ct);
}

// Service Implementation
public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _repository;

    public async Task<PagedResultDto<PermissionDto>> GetPermissionsAsync(
        PermissionQuery query, 
        CancellationToken ct)
    {
        // Step 1: Get total count
        var totalCount = await _repository.CountPermissionsAsync(query.Filters, ct);
        
        // Step 2: Get paginated data (LIMIT/OFFSET executed in SQL)
        var entities = await _repository.GetPagedPermissionsAsync(
            query.PageNumber, 
            query.PageSize, 
            query.Filters, 
            ct
        );
        
        // Step 3: Return PagedResultDto (business layer model)
        return new PagedResultDto<PermissionDto>
        {
            Items = entities.Select(e => new PermissionDto
            {
                PermissionId = e.PermissionId,
                PermissionCode = e.PermissionCode,
                Name = e.Name,
                Description = e.Description
            }),
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}
```

#### Layer 3: Controller (Validates, Maps, Wraps Response)

```csharp
[HttpGet]
[RequirePermission("permission.read")]
public async Task<IActionResult> GetPermissions(
    [FromQuery] PermissionQueryRequest request, 
    CancellationToken ct)
{
    // Step 1: Validate pagination parameters
    if (request.PageNumber < 1)
        return ValidationError("頁碼必須大於 0");
    
    if (request.PageSize < 1 || request.PageSize > 100)
        return ValidationError("每頁筆數必須在 1-100 之間");
    
    // Step 2: Build query from request
    var query = new PermissionQuery 
    { 
        PageNumber = request.PageNumber, 
        PageSize = request.PageSize,
        Filters = new PermissionFilters
        {
            PermissionType = request.PermissionType,
            SearchKeyword = request.SearchKeyword
        }
    };
    
    // Step 3: Call service (returns PagedResultDto<PermissionDto>)
    var pagedResult = await _permissionService.GetPermissionsAsync(query, ct);
    
    // Step 4: Map Service DTO to API Response DTO
    var responseItems = pagedResult.Items
        .Select(dto => new PermissionResponse
        {
            PermissionId = dto.PermissionId,
            PermissionCode = dto.PermissionCode,
            Name = dto.Name,
            Description = dto.Description
        })
        .ToList();
    
    // Step 5: Return wrapped in PagedApiResponseModel using BaseController helper
    return PagedSuccess(
        items: responseItems, 
        pageNumber: pagedResult.PageNumber, 
        pageSize: pagedResult.PageSize, 
        totalCount: pagedResult.TotalCount, 
        message: "查詢成功"
    );
}
```

### 5.2 PagedResultDto Definition

```csharp
// Models/Responses/PagedResultDto.cs
namespace V3.Admin.Backend.Models.Responses;

/// <summary>
/// 分頁結果模型(Service 層使用)
/// </summary>
public class PagedResultDto<T>
{
    /// <summary>
    /// 分頁資料項目
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    
    /// <summary>
    /// 總筆數
    /// </summary>
    public long TotalCount { get; set; }
    
    /// <summary>
    /// 當前頁碼(從 1 開始)
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// 每頁筆數
    /// </summary>
    public int PageSize { get; set; }
}
```

### 5.3 Performance Optimization

**COUNT Query Optimization**:
```sql
-- Use covering index to speed up COUNT queries
CREATE INDEX idx_permissions_isdeleted_createdat 
ON permissions(is_deleted, created_at DESC);

-- Optimized COUNT query
SELECT COUNT(*) 
FROM permissions 
WHERE is_deleted = false; -- Uses index
```

**Pagination Index Strategy**:
```sql
-- Composite index for paginated queries with ORDER BY
CREATE INDEX idx_permissions_pagination 
ON permissions(is_deleted, created_at DESC);

-- Query can use index for both WHERE and ORDER BY
SELECT * 
FROM permissions 
WHERE is_deleted = false 
ORDER BY created_at DESC 
LIMIT 20 OFFSET 40;
```

**Avoid Common Pitfalls**:
- ❌ Never load entire dataset and paginate in-memory: `entities.Skip().Take()`
- ❌ Never return `IQueryable` from Repository to Controller (lazy evaluation breaks layer separation)
- ✅ Always execute `LIMIT/OFFSET` in SQL queries
- ✅ Always calculate `TotalCount` via separate COUNT query before pagination

---

## 6. API Response Patterns

### 6.1 HTTP Status Code + Business Code Mapping

All API responses use a dual-layer design: HTTP status codes reflect request processing state, while business codes provide fine-grained scenario information.

| HTTP Status | Business Code | Scenario | Example Message |
|-------------|---------------|----------|-----------------|
| **200 OK** | `SUCCESS` | Successful operation | "查詢成功" |
| **201 Created** | `SUCCESS` | Resource created | "帳號創建成功" |
| **400 Bad Request** | `VALIDATION_ERROR` | Input validation failure | "帳號長度必須為 3-20 字元" |
| **400 Bad Request** | `PASSWORD_SAME_AS_OLD` | Business rule violation | "新密碼不能與舊密碼相同" |
| **401 Unauthorized** | `INVALID_CREDENTIALS` | Authentication failure | "帳號或密碼錯誤" |
| **401 Unauthorized** | `TOKEN_EXPIRED` | JWT token expired | "登入已過期,請重新登入" |
| **403 Forbidden** | `FORBIDDEN` | Authorization failure | "您只能更新自己的資訊" |
| **403 Forbidden** | `CANNOT_DELETE_SELF` | Business rule violation | "無法刪除自己的帳號" |
| **404 Not Found** | `NOT_FOUND` | Resource not found | "帳號不存在" |
| **409 Conflict** | `CONCURRENT_UPDATE_CONFLICT` | Optimistic locking failure | "資料已被其他使用者修改,請重新整理後再試" |
| **422 Unprocessable Entity** | `USERNAME_EXISTS` | Duplicate resource | "帳號已存在" |
| **422 Unprocessable Entity** | `LAST_ACCOUNT_CANNOT_DELETE` | Business rule violation | "無法刪除最後一個帳號" |
| **500 Internal Server Error** | `INTERNAL_ERROR` | System failure | "系統發生錯誤,請稍後再試" |

### 6.2 BaseController Helper Methods

Controllers MUST use helper methods from `BaseApiController` to create consistent responses:

```csharp
public abstract class BaseApiController : ControllerBase
{
    // Success responses (200 OK)
    protected IActionResult Success<T>(T data, string message = "操作成功")
    {
        return Ok(ApiResponseModel<T>.CreateSuccess(data, message));
    }

    // Created responses (201 Created)
    protected IActionResult Created<T>(T data, string message = "創建成功")
    {
        return StatusCode(201, ApiResponseModel<T>.CreateSuccess(data, message));
    }

    // Validation errors (400 Bad Request)
    protected IActionResult ValidationError(string message)
    {
        return BadRequest(ApiResponseModel<object>.CreateFailure(
            ResponseCodes.VALIDATION_ERROR, 
            message
        ));
    }

    // Not found (404 Not Found)
    protected IActionResult NotFound(string message = "資源不存在")
    {
        return StatusCode(404, ApiResponseModel<object>.CreateFailure(
            ResponseCodes.NOT_FOUND, 
            message
        ));
    }

    // Business errors (422 Unprocessable Entity)
    protected IActionResult BusinessError(string code, string message)
    {
        return StatusCode(422, ApiResponseModel<object>.CreateFailure(code, message));
    }

    // Paginated success (200 OK)
    protected IActionResult PagedSuccess<T>(
        List<T> items, 
        int pageNumber, 
        int pageSize, 
        long totalCount, 
        string message = "查詢成功")
    {
        return Ok(ApiResponseFactory.CreatePagedSuccess(
            items, 
            pageNumber, 
            pageSize, 
            totalCount, 
            message
        ));
    }

    // Internal errors (500 Internal Server Error)
    protected IActionResult InternalError(string message = "系統發生錯誤")
    {
        return StatusCode(500, ApiResponseModel<object>.CreateFailure(
            ResponseCodes.INTERNAL_ERROR, 
            message
        ));
    }
}
```

**Usage Example**:
```csharp
public class AccountController : BaseApiController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Validation error
        if (string.IsNullOrEmpty(request.Username))
            return ValidationError("帳號不能為空");

        var result = await _authService.LoginAsync(request);

        // Authentication failure
        if (result is null)
            return Unauthorized(ApiResponseModel<object>.CreateFailure(
                ResponseCodes.INVALID_CREDENTIALS, 
                "帳號或密碼錯誤"
            ));

        // Success
        return Success(result, "登入成功");
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        // Check username exists
        if (await _accountService.UsernameExistsAsync(request.Username))
            return BusinessError(ResponseCodes.USERNAME_EXISTS, "帳號已存在");

        var account = await _accountService.CreateAsync(request);
        
        // Return 201 Created
        return Created(account, "帳號創建成功");
    }
}
```

---

## 7. Security Implementation

### 7.1 Authentication

#### JWT Token Generation

```csharp
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public string GenerateToken(UserDto user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("DisplayName", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
        );
        var credentials = new SigningCredentials(
            key, 
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1), // 1-hour expiration
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### JWT Token Validation Configuration

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
            ),
            ClockSkew = TimeSpan.Zero // No tolerance for expiration
        };
    });
```

### 7.2 Password Hashing

**BCrypt with Work Factor 12**:

```csharp
using BCrypt.Net;

public class AuthService : IAuthService
{
    // Hash password during registration
    public async Task<UserDto> RegisterAsync(CreateAccountRequest request)
    {
        // BCrypt with work factor 12 (2^12 = 4096 iterations)
        var passwordHash = BCrypt.HashPassword(
            request.Password, 
            workFactor: 12
        );

        var user = new User
        {
            Username = request.Username,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName
        };

        await _userRepository.CreateAsync(user);
        return MapToDto(user);
    }

    // Verify password during login
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        
        if (user is null)
            return null;

        // Verify password with BCrypt
        var isValid = BCrypt.Verify(
            request.Password, 
            user.PasswordHash
        );

        if (!isValid)
            return null;

        var token = _jwtService.GenerateToken(MapToDto(user));
        
        return new LoginResponse
        {
            Token = token,
            Username = user.Username,
            DisplayName = user.DisplayName
        };
    }
}
```

### 7.3 Input Validation with FluentValidation

**Common Validation Rules**:

```csharp
using FluentValidation;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        // Username: 3-20 characters, alphanumeric + underscore
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("帳號不能為空")
            .Length(3, 20).WithMessage("帳號長度必須為 3-20 字元")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("帳號只能包含英數字和底線");

        // Password: minimum 8 characters
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密碼不能為空")
            .MinimumLength(8).WithMessage("密碼長度至少 8 字元");

        // DisplayName: 1-100 characters
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("顯示名稱不能為空")
            .MaximumLength(100).WithMessage("顯示名稱長度不能超過 100 字元");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("舊密碼不能為空");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密碼不能為空")
            .MinimumLength(8).WithMessage("新密碼長度至少 8 字元");

        // Business rule: new password cannot be same as old
        RuleFor(x => x)
            .Must(x => x.NewPassword != x.OldPassword)
            .WithMessage("新密碼不能與舊密碼相同")
            .WithName("NewPassword");
    }
}
```

### 7.4 SQL Injection Prevention

**Always use parameterized queries with Dapper**:

```csharp
// ✅ CORRECT: Parameterized query
public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
{
    var sql = @"
        SELECT id AS Id, username AS Username, password_hash AS PasswordHash
        FROM users 
        WHERE username = @Username AND is_deleted = false";
    
    return await _connection.QueryFirstOrDefaultAsync<User>(
        new CommandDefinition(sql, new { Username = username }, cancellationToken: ct)
    );
}

// ❌ WRONG: String concatenation (SQL injection risk)
public async Task<User?> GetByUsernameAsync(string username)
{
    var sql = $"SELECT * FROM users WHERE username = '{username}'"; // VULNERABLE!
    return await _connection.QueryFirstOrDefaultAsync<User>(sql);
}
```

### 7.5 Sensitive Data Protection

**Logging Best Practices**:

```csharp
// ✅ CORRECT: Never log passwords or tokens
_logger.LogInformation(
    "User {Username} login attempt", 
    request.Username
);

// ❌ WRONG: Logging sensitive data
_logger.LogInformation(
    "Login request: {Request}", 
    JsonSerializer.Serialize(request) // Contains password!
);
```

**Error Response Best Practices**:

```csharp
// ✅ CORRECT: Generic error message
catch (Exception ex)
{
    _logger.LogError(ex, "Error creating account for {Username}", request.Username);
    return InternalError("系統發生錯誤,請稍後再試");
}

// ❌ WRONG: Exposing internal details
catch (Exception ex)
{
    return InternalError($"Database error: {ex.Message}"); // Reveals internal structure!
}
```

---

## 8. Performance Best Practices

### 8.1 Asynchronous Programming Patterns

**Always use async/await for I/O operations**:

```csharp
// ✅ CORRECT: Async database calls
public async Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken ct)
{
    var user = await _userRepository.GetByIdAsync(userId, ct);
    return user is not null ? MapToDto(user) : null;
}

// ❌ WRONG: Blocking synchronous calls
public UserDto? GetUserById(int userId)
{
    var user = _userRepository.GetByIdAsync(userId, CancellationToken.None).Result; // Blocks thread!
    return user is not null ? MapToDto(user) : null;
}
```

**CancellationToken Usage**:

```csharp
public class UserController : BaseApiController
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(
        int id, 
        CancellationToken ct) // Automatically bound from HttpContext
    {
        var user = await _userService.GetUserByIdAsync(id, ct);
        
        if (user is null)
            return NotFound("用戶不存在");
        
        return Success(user);
    }
}

// Pass CancellationToken through all layers
public class UserService : IUserService
{
    public async Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken ct)
    {
        return await _userRepository.GetByIdAsync(userId, ct);
    }
}

public class UserRepository : IUserRepository
{
    public async Task<User?> GetByIdAsync(int userId, CancellationToken ct)
    {
        var sql = "SELECT * FROM users WHERE id = @UserId";
        return await _connection.QueryFirstOrDefaultAsync<User>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct)
        );
    }
}
```

### 8.2 N+1 Query Prevention

**Problem: N+1 Queries**:
```csharp
// ❌ WRONG: Causes N+1 queries
public async Task<List<RoleDetailDto>> GetRolesWithPermissionsAsync(CancellationToken ct)
{
    var roles = await _roleRepository.GetAllAsync(ct); // 1 query
    
    var result = new List<RoleDetailDto>();
    foreach (var role in roles)
    {
        // N queries (one per role)
        var permissions = await _permissionRepository.GetByRoleIdAsync(role.Id, ct);
        result.Add(new RoleDetailDto
        {
            RoleId = role.Id,
            RoleName = role.Name,
            Permissions = permissions.ToList()
        });
    }
    
    return result;
}
```

**Solution: Single Query with JOIN**:
```csharp
// ✅ CORRECT: Single query with JOIN
public async Task<List<RoleDetailDto>> GetRolesWithPermissionsAsync(CancellationToken ct)
{
    var sql = @"
        SELECT 
            r.id AS RoleId,
            r.role_code AS RoleCode,
            r.name AS RoleName,
            p.permission_id AS PermissionId,
            p.permission_code AS PermissionCode,
            p.name AS PermissionName
        FROM roles r
        LEFT JOIN role_permissions rp ON r.id = rp.role_id
        LEFT JOIN permissions p ON rp.permission_id = p.permission_id
        WHERE r.is_deleted = false AND (p.is_deleted = false OR p.is_deleted IS NULL)
        ORDER BY r.id";
    
    var roleDict = new Dictionary<int, RoleDetailDto>();
    
    await _connection.QueryAsync<RoleDetailDto, PermissionDto, RoleDetailDto>(
        new CommandDefinition(sql, cancellationToken: ct),
        (role, permission) =>
        {
            if (!roleDict.TryGetValue(role.RoleId, out var existingRole))
            {
                existingRole = role;
                existingRole.Permissions = new List<PermissionDto>();
                roleDict.Add(role.RoleId, existingRole);
            }
            
            if (permission is not null)
                existingRole.Permissions.Add(permission);
            
            return existingRole;
        },
        splitOn: "PermissionId"
    );
    
    return roleDict.Values.ToList();
}
```

### 8.3 Database Query Optimization

**Use Indexes for Frequent Queries**:

```sql
-- Index for username lookups (login queries)
CREATE UNIQUE INDEX idx_users_username ON users(username) WHERE is_deleted = false;

-- Index for pagination queries
CREATE INDEX idx_permissions_pagination ON permissions(is_deleted, created_at DESC);

-- Composite index for filtered queries
CREATE INDEX idx_auditlogs_operatorid_createdat 
ON audit_logs(operator_id, created_at DESC);
```

**Dapper Query Performance**:

```csharp
// ✅ CORRECT: Efficient parameterized query
var sql = @"
    SELECT id, username, display_name 
    FROM users 
    WHERE is_deleted = false 
    ORDER BY created_at DESC 
    LIMIT @Limit";

var users = await _connection.QueryAsync<User>(
    new CommandDefinition(sql, new { Limit = 100 }, cancellationToken: ct)
);

// ❌ WRONG: Selecting unnecessary columns
var sql = "SELECT * FROM users"; // Fetches password_hash, row_version, etc.
```

### 8.4 Response Time Targets

**Performance SLA**:
- **Simple operations** (login, single record query): < 200ms
- **Complex operations** (paginated lists, multi-table joins): < 2000ms

**Monitoring Example**:

```csharp
public class AuditLogService : IAuditLogService
{
    private readonly ILogger<AuditLogService> _logger;
    private readonly Stopwatch _stopwatch = new();

    public async Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(
        AuditLogQuery query, 
        CancellationToken ct)
    {
        _stopwatch.Restart();
        
        try
        {
            var result = await _repository.GetPagedAsync(query, ct);
            return result;
        }
        finally
        {
            _stopwatch.Stop();
            
            if (_stopwatch.ElapsedMilliseconds > 2000)
            {
                _logger.LogWarning(
                    "Slow query detected: GetAuditLogsAsync took {ElapsedMs}ms (threshold: 2000ms)",
                    _stopwatch.ElapsedMilliseconds
                );
            }
        }
    }
}
```

---

## Document Revision History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-12-13 | Initial creation - Extracted technical details from Constitution v1.10.1 |

---

**Related Documents**:
- [Constitution](constitution.md) - Core governance principles and requirements
- [API Specification](../specs/V3.Admin.Backend.API.yaml) - OpenAPI documentation
