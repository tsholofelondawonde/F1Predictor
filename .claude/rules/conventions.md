# Conventions

## File Naming

- `F1Predictor.Application/Features/[Area]/[UseCaseName]/`, one folder per use case:
  `{UseCaseName}Command.cs` or `Query.cs`, `{UseCaseName}CommandHandler.cs` or
  `QueryHandler.cs`, `{UseCaseName}CommandValidator.cs` (commands only — queries have
  no validators in this codebase), and an optional `{UseCaseName}Response.cs`.
  Per-area shared files (e.g. `PredictionErrors.cs`) sit one level up, in the area
  folder, not inside a use-case folder.
- `F1Predictor.WebApi/Endpoints/[Area]/{VerbNoun}.cs` — one sealed class per endpoint
  implementing `IEndpoint`, filename equals class name, no `Endpoint` suffix (e.g.
  `GetHoldout.cs`, `Ingest.cs`, `Train.cs`).

## Code Conventions

- File-scoped namespaces throughout.
- Primary constructors for constructor injection on handlers, endpoints, and
  infrastructure services.
- `sealed` on every implementation class — handlers, endpoints, DTOs.
- `internal` by default; `public` only for things that cross a layer boundary
  (`Abstractions/` interfaces, and command/query/response DTOs referenced from other
  projects or serialized in responses).
- Commands/queries/responses are positional records (`public sealed record
  GetHoldoutPredictionsQuery(int Year) : IQuery<HoldoutPredictionsResponse>;`), except
  a command whose only input is a route-bound primitive, which is a mutable class with
  a `{ get; set; }` property instead (e.g. `IngestSeasonCommand.Year`) — needed for
  minimal-API parameter binding.
- Mapping between DTOs and domain/EF types is manual (`new FooResponse(...)`) — no
  mapping library.

## Patterns We Use

- `Result<T>` (`SharedKernel/Result.cs`) as the return type of every handler; `Error`
  (`SharedKernel/Error.cs`) with `Code`/`Description`/`Type`/`UserMessage` and static
  factories (`NotFound`, `Problem`, `Conflict`, `ValidationFailure`).
- `ICommandHandler<>`/`IQueryHandler<>`, auto-registered by Scrutor assembly scan —
  see DI Registration below.
- `IEndpoint` + assembly scan for route registration.
- FluentValidation validators, auto-registered via `AddValidatorsFromAssembly`, run by
  `ValidationDecorator` ahead of the handler.
- `LoggingDecorator` wrapping both command and query handlers.
- `IApplicationDbContext` injected directly into handlers for data access.

## Patterns We Do NOT Use

- MediatR — deliberate; replaced by the scaffold's own handler interfaces.
- Repository pattern — handlers use `IApplicationDbContext` directly, no
  `IFooRepository` layer.
- AutoMapper or any mapping library — mapping is written by hand.
- Exceptions for business-rule failures — those return `Result.Failure(...)`.
  `throw` is reserved for genuine programmer errors (invalid enum switches, DI/wiring
  failures) and one pre-`Result`-boundary guard in `MlNetRacePredictor` that the
  calling handler is expected to check for first (`predictor.ModelsAvailable`).

## DI Registration

- `F1Predictor.Application/DependencyInjection.cs` → `AddApplication()`: Scrutor
  `services.Scan(...)` over the Application assembly registers every
  `ICommandHandler<>`, `ICommandHandler<,>`, `IQueryHandler<,>`, and
  `IDomainEventHandler<>` implementation as `AsImplementedInterfaces()`,
  `WithScopedLifetime()`, `publicOnly: false` (so `internal` handlers are included).
- Same method: `AddValidatorsFromAssembly(assembly, includeInternalTypes: true)`
  auto-registers every `AbstractValidator<T>`.
- Same method: `services.Decorate(...)` applies `ValidationDecorator.CommandHandler`
  first (innermost), then `LoggingDecorator.QueryHandler` and
  `LoggingDecorator.CommandHandler` (outermost).
- `F1Predictor.WebApi/DependencyInjection.cs` → `AddPresentation()`:
  `services.AddEndpoints(assembly)` reflects over the WebApi assembly for `IEndpoint`
  implementations and registers each transient via `TryAddEnumerable`.
- `Program.cs` calls `app.MapEndpoints()`, which resolves `IEnumerable<IEndpoint>` and
  calls `MapEndpoint(app)` on each — no per-route calls live in `Program.cs`.
