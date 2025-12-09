# V3.Admin.Backend - Admin Management System Backend

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-46_Passing-success)](Tests/)

English | [繁體中文](README.md)

Modern admin management system backend API built with ASP.NET Core 10 and PostgreSQL. This project pairs with [V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite) frontend to rapidly create a complete enterprise-grade admin system with authentication, permission management, role-based access control, and more.

## 🎯 Project Features

This project is designed as the backend system for the **[V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite)** frontend framework, enabling rapid development of enterprise-grade admin systems with:

- 🚀 **Ready to Use** - Complete full-stack integration solution for rapid project kickoff
- 🎨 **Modern Frontend** - Vue 3 + TypeScript + Element Plus management interface
- ⚡ **High-Performance Backend** - .NET 10 + PostgreSQL delivering stable, efficient API services
- 🔐 **Complete Permission System** - RBAC role-based access control with fine-grained permissions

## ✨ Key Features

- 🔐 **JWT Authentication** - Stateless Bearer Token-based authentication
- 👤 **Account Management** - Complete CRUD operations (Create, Read, Update, Delete)
- 🔑 **Password Management** - BCrypt hashing (work factor 12) + password change
- 🛡️ **Security** - Input validation, SQL injection protection, soft delete mechanism
- 🔄 **Concurrency Control** - Optimistic locking to prevent data conflicts
- 📝 **Comprehensive Logging** - Structured logging with TraceId tracking
- 📚 **API Documentation** - Built-in Swagger UI interactive documentation
- ✅ **High Test Coverage** - 42 unit tests + 4 integration tests (100% passing)
- 🌐 **Traditional Chinese** - Complete Traditional Chinese error messages and documentation
- 🐳 **Docker Support** - Container deployment ready

## 🔗 Related Projects

- **Frontend Project**: [V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite) - Vue 3 + TypeScript + Element Plus Admin Dashboard

This project provides a complete RESTful API that seamlessly integrates with the frontend project to rapidly build enterprise-grade admin management systems.

## 🚀 Quick Start

### Prerequisites

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Optional, for integration tests)

### Installation Steps

1. **Clone the Repository**
   ```powershell
   git clone https://github.com/chuangmaster/V3.Admin.Backend.git
   cd V3.Admin.Backend
   git checkout 001-account-management
   ```

2. **Setup Database**
   ```powershell
   # Create database
   psql -U postgres -c "CREATE DATABASE v3admin_dev;"
   
   # Run migrations
   cd Database/Migrations
   Get-ChildItem -Filter "*.sql" | Sort-Object Name | ForEach-Object {
       psql -U postgres -d v3admin_dev -f $_.FullName
   }
   cd ../..
   
   # Insert test data
   psql -U postgres -d v3admin_dev -f Database/Scripts/seed.sql
   ```

3. **Configure Settings**
   
   Edit `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=v3admin_dev;Username=postgres;Password=postgres"
     },
     "JwtSettings": {
       "SecretKey": "YourSecretKeyAtLeast32Characters!!!",
       "Issuer": "V3.Admin.Backend",
       "Audience": "V3.Admin.Frontend",
       "ExpirationMinutes": 60
     }
   }
   ```

4. **Start the Application**
   ```powershell
   dotnet run
   ```
   
   Open browser at `https://localhost:5001/swagger`

### Default Test Accounts

- Username: `admin` / Password: `Admin@123`
- Username: `testuser` / Password: `Test@123`

## 📖 API Endpoints

### Authentication

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/login` | User login | ❌ |

### Account Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/accounts` | Query account list (paginated) | ✅ |
| GET | `/api/accounts/{id}` | Query single account | ✅ |
| POST | `/api/accounts` | Create account | ✅ |
| PUT | `/api/accounts/{id}` | Update account info | ✅ |
| PUT | `/api/accounts/{id}/password` | Change password | ✅ |
| DELETE | `/api/accounts/{id}` | Delete account (soft delete) | ✅ |

### API Usage Examples

#### Login
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
```

#### Create Account
```bash
curl -X POST https://localhost:5001/api/accounts \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"username":"newuser","password":"Secure@123","displayName":"New User"}'
```

For complete API documentation and examples, refer to [Swagger UI](https://localhost:5001/swagger) or [Quickstart Guide](specs/001-account-management/quickstart.md).

## 🏗️ Technical Architecture

### Technology Stack

- **Framework**: ASP.NET Core 10.0 (Web API)
- **Language**: C# 14
- **Database**: PostgreSQL 15+
- **ORM**: Dapper (Micro-ORM)
- **Authentication**: JWT Bearer Token
- **Password Hashing**: BCrypt.Net-Next
- **Input Validation**: FluentValidation
- **API Documentation**: Swagger/OpenAPI
- **Testing Framework**: xUnit + Moq + FluentAssertions + Testcontainers

### Project Structure

```
V3.Admin.Backend/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── AccountController.cs
│   └── BaseApiController.cs
├── Services/             # Business Logic Layer
│   ├── AuthService.cs
│   ├── AccountService.cs
│   └── JwtService.cs
├── Repositories/         # Data Access Layer
│   └── UserRepository.cs
├── Models/               # Data Models
│   ├── Entities/         # Database Entities
│   ├── Dtos/             # Data Transfer Objects
│   ├── Requests/         # API Request Models
│   └── Responses/        # API Response Models
├── Validators/           # FluentValidation Validators
├── Middleware/           # Middleware
│   ├── ExceptionHandlingMiddleware.cs
│   └── TraceIdMiddleware.cs
├── Configuration/        # Configuration Models
├── Database/             # Database Scripts
│   ├── Migrations/       # Migration Scripts
│   └── Scripts/          # Seed Data
└── Tests/                # Test Projects
    ├── Unit/             # Unit Tests (42 tests)
    └── Integration/      # Integration Tests (4 tests)
```

### Architecture Design

- **Three-Layer Architecture**: Controller → Service → Repository
- **Dependency Injection**: Using ASP.NET Core DI container
- **DTO Pattern**: Separation of internal models and API contracts
- **Repository Pattern**: Abstract data access logic
- **Middleware Pipeline**: Centralized exception handling and TraceId
- **Unified Response Format**: `ApiResponseModel<T>` wraps all responses

### API Response Format

All API responses follow a unified format:

```json
{
  "success": true,
  "code": "SUCCESS",
  "message": "Operation successful",
  "data": { ... },
  "timestamp": "2025-10-28T10:30:00Z",
  "traceId": "7d3e5f8a-2b4c-4d9e-8f7a-1c2d3e4f5a6b"
}
```

HTTP Status Code and Business Code Mapping:

| HTTP | Business Code | Description |
|------|--------------|-------------|
| 200 | SUCCESS | Operation successful |
| 201 | CREATED | Resource created successfully |
| 400 | VALIDATION_ERROR | Input validation error |
| 401 | UNAUTHORIZED / INVALID_CREDENTIALS | Unauthorized / Invalid credentials |
| 404 | NOT_FOUND | Resource not found |
| 409 | CONCURRENT_UPDATE_CONFLICT | Concurrent update conflict |
| 422 | USERNAME_EXISTS / ... | Business logic error |
| 500 | INTERNAL_ERROR | Internal system error |

## 🧪 Testing

### Run Tests

```powershell
# Run all tests (46 tests)
dotnet test

# Run only unit tests (42 tests)
dotnet test --filter "FullyQualifiedName!~Integration"

# Run only integration tests (4 tests, requires Docker)
dotnet test --filter "FullyQualifiedName~Integration"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Test Coverage

| Class | Tests | Status |
|-------|-------|--------|
| Validators (LoginRequest) | 7 | ✅ |
| Validators (CreateAccountRequest) | 7 | ✅ |
| Validators (UpdateAccountRequest) | 6 | ✅ |
| Validators (ChangePasswordRequest) | 6 | ✅ |
| Validators (DeleteAccountRequest) | 2 | ✅ |
| Services (AuthService) | 4 | ✅ |
| Integration (AuthController) | 4 | ✅ |
| **Total** | **46** | **✅ 100%** |

### Integration Tests

Integration tests use **Testcontainers** to automatically spin up PostgreSQL containers, eliminating manual test database setup:

```powershell
# Ensure Docker Desktop is running
docker ps

# Run integration tests (automatically creates PostgreSQL container)
dotnet test --filter "FullyQualifiedName~Integration"
```

## 📋 Development Guide

### Coding Conventions

- Follow [C# Coding Conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use XML doc comments for all public members
- All error messages in Traditional Chinese
- Use `async/await` for asynchronous operations
- Repository methods must have corresponding unit tests

### Adding Features

1. Create a new feature branch from `main`
2. Implement features and write tests (TDD recommended)
3. Ensure all tests pass (`dotnet test`)
4. Update API documentation (Swagger comments)
5. Submit Pull Request

### Git Workflow

```powershell
# Create feature branch
git checkout -b feature/your-feature-name

# Commit changes
git add .
git commit -m "feat: add XXX feature"

# Push to remote
git push origin feature/your-feature-name
```

### Commit Message Format

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation update
- `test:` Test-related
- `refactor:` Code refactoring
- `perf:` Performance improvement
- `chore:` Miscellaneous tasks

## 🔒 Security

### Implemented Security Measures

- ✅ JWT Bearer Token authentication
- ✅ BCrypt password hashing (work factor 12)
- ✅ SQL injection protection (parameterized queries)
- ✅ Input validation (FluentValidation)
- ✅ Soft delete mechanism (data audit)
- ✅ Optimistic locking (prevent concurrent conflicts)
- ✅ HTTPS enforcement
- ✅ Unified error handling (prevent information leakage)

### Security Recommendations

- 🔐 **Production JWT SecretKey** must be stored in environment variables or Azure Key Vault
- 🔐 **Database connection strings** should not be included in source code
- 🔐 **Enable HTTPS** and use valid certificates
- 🔐 **Configure CORS** to restrict allowed origins
- 🔐 **Implement rate limiting** to prevent brute force attacks
- 🔐 **Regular updates** of dependencies to patch security vulnerabilities

## 📚 Documentation

- **[Quickstart Guide](specs/001-account-management/quickstart.md)** - Complete installation and usage tutorial
- **[Feature Specification](specs/001-account-management/spec.md)** - User stories and acceptance criteria
- **[Implementation Plan](specs/001-account-management/plan.md)** - 64-item task checklist
- **[API Specification](specs/001-account-management/contracts/api-spec.yaml)** - OpenAPI 3.0 specification
- **[Swagger UI](https://localhost:5001/swagger)** - Interactive API documentation (requires running application)

## 🤝 Contributing

Contributions welcome! This project is co-maintained with the [V3 Admin Vite](https://github.com/chuangmaster/v3-admin-vite) frontend project, committed to providing the best full-stack admin solution.

Please follow these steps:

1. Fork this project
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'feat: add some feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

Please ensure:
- All tests pass
- New features have corresponding tests
- Follow project coding conventions
- Update relevant documentation

## 📝 Version History

### v1.0.0 (2025-10-28)

**Features**:
- ✅ JWT authentication system
- ✅ Account create, read, update, delete
- ✅ Password change functionality
- ✅ Soft delete mechanism
- ✅ Optimistic locking concurrency control
- ✅ Complete input validation
- ✅ 46 tests (100% passing)
- ✅ Swagger API documentation
- ✅ Docker support

**Known Limitations**:
- Role permission management not supported (planned for v2.0)
- Password reset email not supported (planned for v2.0)
- Two-factor authentication not supported (planned for v3.0)

## 📄 License

This project is licensed under the [MIT License](LICENSE) - see LICENSE file for details

## 🌟 Related Resources

- [V3 Admin Vite (Frontend)](https://github.com/chuangmaster/v3-admin-vite) - Vue 3 Admin Dashboard Frontend Project
- [Online Documentation](https://github.com/chuangmaster/V3.Admin.Backend/wiki) - Detailed development documentation
- [Issue Tracker](https://github.com/chuangmaster/V3.Admin.Backend/issues) - Report issues or feature requests

---

⭐ If this project helps you, please give us a Star! Also check out the companion [frontend project](https://github.com/chuangmaster/v3-admin-vite)!
