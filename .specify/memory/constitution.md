<!--
Sync Impact Report:
Version change: 1.0.0 → 1.1.0 (Project Context Addition)
Added sections: Project Background section
Modified principles: Enhanced principle IV and V to include role/permission management context
Templates requiring updates: ✅ All templates aligned with updated constitution
Follow-up TODOs: None - all placeholders filled
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
All API responses MUST use BaseResponseModel wrapper with consistent Success, Message, and Data properties. Error responses MUST follow standardized format with appropriate HTTP status codes. Authentication flows MUST provide clear, actionable error messages in Traditional Chinese. API endpoints MUST implement proper validation with meaningful error responses. Role and permission management operations MUST provide detailed feedback on access violations or configuration conflicts. Response times MUST be predictable and documented. Admin interface operations MUST maintain consistent patterns across all user, role, and permission management endpoints.

**Rationale**: Provides predictable, reliable API behavior that enables consistent frontend integration and positive administrative user experience across all management functions.

### V. Performance & Security Standards for User Management
API endpoints MUST respond within 200ms for simple operations and 2000ms for complex operations including role lookups and permission validations. Asynchronous programming patterns are mandatory for I/O operations. JWT authentication MUST be properly implemented with secure token generation and validation. Role-based authorization MUST be enforced at the API level with proper claim validation. All user inputs MUST be validated and sanitized. Sensitive information (passwords, tokens, personal data) MUST NOT be logged or exposed in error messages. Database queries MUST be optimized to prevent N+1 problems, especially for role/permission hierarchies. Permission checks MUST be cached appropriately to ensure performance under load.

**Rationale**: Ensures application remains responsive under administrative load while maintaining security standards appropriate for user management and access control systems.

## Security Requirements

**Authentication**: JWT Bearer token authentication is mandatory for protected endpoints. Tokens MUST expire within reasonable timeframes (default: 1 hour). Refresh token mechanism MUST be implemented for production use. Multi-factor authentication SHOULD be supported for administrative accounts.

**Authorization**: Role-based and policy-based authorization MUST be implemented using ASP.NET Core authorization framework. All endpoints MUST explicitly declare their authorization requirements. Permission inheritance and role hierarchies MUST be properly validated. Administrative functions require elevated permissions with audit logging.

**Input Validation**: All user inputs MUST be validated using FluentValidation or data annotations. SQL injection prevention MUST be ensured through parameterized queries or ORM usage. Special attention to user management fields (usernames, emails, role names) for injection attacks.

**Data Protection**: User personal data MUST be handled according to privacy regulations. Password storage MUST use secure hashing (bcrypt/scrypt/Argon2). Audit logs MUST be maintained for all user, role, and permission changes. Session management MUST prevent concurrent unauthorized access.

**Error Handling**: Global exception handling middleware MUST be implemented. Sensitive information MUST NOT be exposed in production error responses. All errors MUST be logged with appropriate detail levels including security-relevant events.

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

**Version**: 1.1.0 | **Ratified**: 2025-10-25 | **Last Amended**: 2025-10-25
