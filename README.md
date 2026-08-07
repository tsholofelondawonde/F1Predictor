# F1Predictor

A Clean Architecture .NET solution scaffolded by [CleanArchitectureGenerator](https://www.cleanarchitecturegenerator.com/).

## Architecture

The solution follows Clean Architecture principles with four layers:

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `F1Predictor.Domain` | Entities, value objects, domain events |
| Application | `F1Predictor.Application` | Use cases, interfaces, DTOs |
| Infrastructure | `F1Predictor.Infrastructure` | EF Core, repositories, external services |
| Presentation | `F1Predictor.WebApi` | Minimal APIs, middleware |

## Tech Stack

- **.NET 10** — runtime
- **ASP.NET Core** — web framework
- **Entity Framework Core** — ORM
- **PostgreSQL** — database provider
- **CQRS** —  Custom mediator
- **FluentValidation** — request validation
- **.NET Aspire** — app orchestration and observability
- **OpenTelemetry** — distributed tracing, metrics, and logging
## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `dotnet tool install --global dotnet-ef`
- A running **PostgreSQL** instance

## Getting Started

1. Update the connection string in `F1Predictor.WebApi/appsettings.json`
2. Add the initial migration:
   ```
   dotnet ef migrations add InitialCreate --project F1Predictor.Infrastructure --startup-project F1Predictor.WebApi
   ```
3. Apply migrations:
   ```
   dotnet ef database update --project F1Predictor.Infrastructure --startup-project F1Predictor.WebApi
   ```
4. Run the API:
   ```
   dotnet run --project F1Predictor.AppHost
   ```
5. Open the Scalar UI at `https://localhost:<port>/scalar`

## Project Structure

```
F1Predictor/
├── F1Predictor.AppHost/         # Aspire orchestration entry point
├── F1Predictor.ServiceDefaults/  # Shared telemetry and health checks├── F1Predictor.Domain/          # Entities, domain logic
├── F1Predictor.Application/     # Use cases, interfaces
├── F1Predictor.Infrastructure/  # EF Core, repositories
├── F1Predictor.WebApi/          # (Minimal APIs, startup
└── F1Predictor.slnx
```