# Task Service API

A lightweight REST API for managing tasks, built with **.NET 10** (meets the .NET 8+ requirement) using a clean layered architecture.

## Solution Structure

```
TaskService/
├── src/
│   ├── TaskService.Domain/          # Entities and enums
│   ├── TaskService.Application/     # Business logic, DTOs, interfaces
│   ├── TaskService.Infrastructure/  # EF Core + SQLite persistence
│   └── TaskService.Api/             # REST API, middleware, logging
└── tests/
    └── TaskService.Tests/           # xUnit integration tests
```

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download) (or .NET 8+ SDK)

## Run the API

```bash
cd TaskService
dotnet restore
dotnet run --project src/TaskService.Api
```

The API starts on `https://localhost:7xxx` (see console output). OpenAPI is available in Development at `/openapi/v1.json`.

SQLite database file: `taskservice.db` (created automatically on first run).

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/tasks` | Create a task |
| GET | `/api/tasks/{id}` | Get a task by id |
| GET | `/api/tasks` | List all tasks |
| PUT | `/api/tasks/{id}` | Update a task |
| DELETE | `/api/tasks/{id}` | Delete a task |

### Task Model

| Field | Type | Notes |
|-------|------|-------|
| id | GUID | Auto-generated |
| title | string | Required, max 200 chars |
| description | string | Optional, max 2000 chars |
| status | enum | `Todo`, `InProgress`, `Done` (serialized as strings) |
| originalEstimatedWork | decimal | Must be >= 0 |
| createdAt | datetime (UTC) | Set on create |
| updatedAt | datetime (UTC) | Set on create/update |

### Example Requests

**Create task**
```http
POST /api/tasks
Content-Type: application/json

{
  "title": "Write unit tests",
  "description": "Cover CRUD lifecycle",
  "status": "Todo",
  "originalEstimatedWork": 4
}
```

**Update task**
```http
PUT /api/tasks/{id}
Content-Type: application/json

{
  "title": "Write unit tests",
  "description": "Cover CRUD lifecycle",
  "status": "InProgress",
  "originalEstimatedWork": 6
}
```

### Error Response Format

All errors return a consistent shape:

```json
{
  "statusCode": 400,
  "message": "Validation failed.",
  "errors": {
    "Title": ["The Title field is required."]
  },
  "traceId": "..."
}
```

## Run Tests

```bash
dotnet test
```

## Step-by-Step Development Process

1. **Define the domain** — Created `TaskItem` entity and `TaskStatus` enum in the Domain layer.
2. **Design contracts** — Added DTOs (`CreateTaskRequest`, `UpdateTaskRequest`, `TaskResponse`) and repository/service interfaces in Application.
3. **Implement business logic** — Built `TaskService` with CRUD operations, UTC timestamps, and `NotFoundException` for missing tasks.
4. **Add persistence** — Used EF Core with SQLite, `TaskDbContext`, and `TaskRepository` implementing `ITaskRepository`.
5. **Expose REST endpoints** — Created `TasksController` with standard HTTP verbs and status codes.
6. **Validation & errors** — Applied DataAnnotations on DTOs; customized 400 responses; added global exception middleware for 404/500.
7. **Logging** — Configured console logging with structured log messages in the controller.
8. **Testing** — Wrote xUnit integration tests using `WebApplicationFactory` and in-memory SQLite.

## Architecture & SOLID Notes

- **Single Responsibility** — Each layer has one concern (domain, application logic, persistence, HTTP).
- **Open/Closed** — New storage providers can be added by implementing `ITaskRepository` without changing controllers.
- **Liskov Substitution** — Services depend on interfaces, not concrete implementations.
- **Interface Segregation** — Separate `ITaskRepository` and `ITaskService` interfaces.
- **Dependency Inversion** — API depends on abstractions; Infrastructure is wired via DI extension methods.

## Assumptions & Trade-offs

| Decision | Rationale |
|----------|-----------|
| **SQLite** | Zero-config persistence; easy to demo locally. Swap to SQL Server/PostgreSQL by changing connection string and provider. |
| **Layered architecture (4 projects)** | Demonstrates structure without over-engineering. Could be simplified to 1–2 projects for a smaller scope. |
| **EnsureCreated vs migrations** | Faster for a timeboxed exercise; production would use EF migrations. |
| **No auth** | Explicitly out of scope per requirements. |
| **Integration tests over unit tests** | Better demonstrates end-to-end API behavior with minimal test setup. |
| **.NET 10 target** | Only .NET 10 SDK available on build machine; fully compatible with .NET 8+ patterns. |

## Possible Extensions (Follow-up Discussion)

- Pagination and filtering on list endpoint
- Soft delete and audit trail
- JWT authentication
- EF Core migrations and CI pipeline
- Domain events for status transitions
- FluentValidation for richer rules
