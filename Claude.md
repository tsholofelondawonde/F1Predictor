# F1 Race Predictor

## 1. Overview

A Clean Architecture .NET solution that predicts F1 race outcomes: it ingests historical
race data from OpenF1, engineers a small feature set, trains two binary classifiers
(podium probability, points-finish probability), and serves both training and predictions
over a minimal API, with a Next.js frontend on top.

It also looks forwards, not just back: it previews the next Grand Prix, keeps both
championship tables, and simulates the rest of the season to give each driver and
constructor a title probability — see sections 12a and 12b.

This started as a flat console walking skeleton and has since been ported onto the layered
scaffold produced by [CleanArchitectureGenerator](https://www.nuget.org/packages/CleanArchitectureGenerator)
(`cleanarch new F1Predictor -d postgres --aspire`). The pipeline itself is unchanged — the
same features, the same SDCA classifiers, the same holdout check — but each part now lives
in the layer that owns it, and the console's `Console.WriteLine` reporting has become
`Result<T>`-returning use cases behind HTTP endpoints.

Still deliberately out of scope: multi-season training, tyre-strategy features, and any
ranking model over race results. Those remain the natural next steps.

## 2. Tech Stack

| Concern | Choice |
|---|---|
| Framework | .NET 10, minimal APIs |
| Orchestration | .NET Aspire (`F1Predictor.AppHost`) |
| Database | PostgreSQL, hosted on [Neon](https://neon.tech) |
| ORM | EF Core 10 + Npgsql |
| CQRS | Scaffold's own `ICommandHandler<T,R>` / `IQueryHandler<T,R>` (no MediatR) |
| Validation | FluentValidation, applied by a handler decorator |
| ML | ML.NET 5 — `SdcaLogisticRegression`, not AutoML |
| Championship odds | Plackett–Luce + Monte Carlo, hand-rolled in the Domain (see §12b) |
| API docs | Scalar over OpenAPI, at `/scalar` |
| Frontend | Next.js 16 App Router, React 19, Tailwind 4, axios, zustand |

There is no container runtime on the development machine, so Aspire references Neon as an
external connection string rather than provisioning a Postgres container.

## 3. Project Structure

```
F1Predictor/
├── SharedKernel/                   Entity base, Result<T>, Error, domain event contracts
├── F1Predictor.Domain/
│   ├── RaceData/Entities/          Meeting, RaceSession, StartingGridEntry,
│   │                               SessionResultEntry, PitStopEntry, WeatherReading,
│   │                               DriverEntry
│   ├── Predictions/                DriverRaceFeature + the feature engineering rules
│   └── Championship/               Points scale, standings + count-back, Plackett-Luce
│                                   form fit, Monte Carlo simulator, title scenarios
├── F1Predictor.Application/
│   ├── Abstractions/               IOpenF1Client, IModelTrainer, IRacePredictor,
│   │                               IApplicationDbContext, ILegacyDatabaseImporter
│   └── Features/                   Use cases, one folder per command/query
├── F1Predictor.Infrastructure/
│   ├── Database/                   ApplicationDbContext, configurations, migrations
│   ├── OpenF1/                     Typed HTTP client + politeness delay handler
│   ├── MachineLearning/            ML.NET trainer and predictor
│   └── Legacy/                     One-off SQLite → Postgres import
├── F1Predictor.WebApi/Endpoints/   One sealed class per endpoint
├── F1Predictor.Web/                Next.js frontend (not in the .slnx — AppHost runs it)
│   └── src/{app,features,shared}/  Routes, feature slices, shared components
├── F1Predictor.AppHost/            Aspire orchestration
└── F1Predictor.ServiceDefaults/    Aspire telemetry/health defaults
```

`F1Predictor.Web` is deliberately absent from `F1Predictor.slnx` — it is a Node app, wired up
by `AddJavaScriptApp` in the AppHost, which also injects `NEXT_PUBLIC_API_URL`. Its own
`AGENTS.md` warns that **Next.js 16 differs from what a model is likely to remember**; read
the guides under `F1Predictor.Web/node_modules/next/dist/docs/` before writing route or
layout code. Notably, `params` and `searchParams` are Promises and pages are typed with the
generated `PageProps<"/route">` helper.

Dependencies point inward only. Domain references nothing but `SharedKernel`; Application
defines the ports; Infrastructure implements them; WebApi composes.

## 4. Architecture

- Clean Architecture, dependencies point inward only (Domain → Application →
  Infrastructure/WebApi — see section 3).
- CQRS via the scaffold's own `ICommandHandler<TCommand>` / `ICommandHandler<TCommand,
  TResponse>` / `IQueryHandler<TQuery, TResponse>` (`F1Predictor.Application/
  Abstractions/Messaging/`) — no MediatR.
- Cross-cutting concerns are applied as decorators (Scrutor `services.Decorate`), not
  pipeline behaviors: `ValidationDecorator.CommandHandler` wraps commands only
  (queries aren't validated), `LoggingDecorator` wraps both commands and queries.
  Validation decorates innermost, logging outermost, so logging sees the
  already-validated call.
- WebApi endpoints are classes implementing a marker `IEndpoint` interface
  (`F1Predictor.WebApi/Endpoints/IEndpoint.cs`), discovered by assembly scan and
  dispatched via `app.MapEndpoints()` — there is no central route table in `Program.cs`.

## 5. File Naming

- `F1Predictor.Application/Features/[Area]/[UseCaseName]/`, one folder per use case:
  `{UseCaseName}Command.cs` or `Query.cs`, `{UseCaseName}CommandHandler.cs` or
  `QueryHandler.cs`, `{UseCaseName}CommandValidator.cs` (commands only — queries have
  no validators in this codebase), and an optional `{UseCaseName}Response.cs`.
  Per-area shared files (e.g. `PredictionErrors.cs`) sit one level up, in the area
  folder, not inside a use-case folder.
- `F1Predictor.WebApi/Endpoints/[Area]/{VerbNoun}.cs` — one sealed class per endpoint
  implementing `IEndpoint`, filename equals class name, no `Endpoint` suffix (e.g.
  `GetHoldout.cs`, `Ingest.cs`, `Train.cs`).

## 6. Code Conventions

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

## 7. Patterns We Use

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

## 8. Patterns We Do NOT Use

- MediatR — deliberate; replaced by the scaffold's own handler interfaces.
- Repository pattern — handlers use `IApplicationDbContext` directly, no
  `IFooRepository` layer.
- AutoMapper or any mapping library — mapping is written by hand.
- Exceptions for business-rule failures — those return `Result.Failure(...)`.
  `throw` is reserved for genuine programmer errors (invalid enum switches, DI/wiring
  failures) and one pre-`Result`-boundary guard in `MlNetRacePredictor` that the
  calling handler is expected to check for first (`predictor.ModelsAvailable`).

## 9. DI Registration

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

## 10. Data Source: OpenF1

Base URL `https://api.openf1.org/v1/`, no auth, **2023 onwards only** on the free tier.

| Endpoint | Query by | Key fields |
|---|---|---|
| `meetings` | `year` | `meeting_key`, `year`, `circuit_short_name`, `country_name`, `meeting_name`, `date_start` |
| `sessions` | `meeting_key` | `session_key`, `meeting_key`, `session_name`, `session_type`, `date_start` |
| `starting_grid` | **qualifying** `session_key` | `position`, `driver_number`, `lap_duration` |
| `session_result` | **race** `session_key` | `driver_number`, `position` (nullable), `points`, `dnf`, `dns`, `dsq` |
| `pit` | **race** `session_key` | `driver_number`, `stop_duration` (null before the 2024 US GP — falls back to `lane_duration`), `lane_duration` |
| `weather` | **race** `session_key` | `rainfall` (0/1), `track_temperature` |
| `drivers` | any `session_key` | `driver_number`, `full_name`, `name_acronym`, `team_name`, `team_colour` |

**The grid comes from the qualifying session, not the race session.** This was verified
against live responses and contradicts what the original plan assumed, which is why
`RaceSession` carries a `QualifyingSessionKey` pointer. A sprint takes its order from the
**Sprint Qualifying** session instead — also verified live.

**Sprints arrive down the same pipe as a Grand Prix**: OpenF1 gives them
`session_type: "Race"` with `session_name: "Sprint"`. They award championship points
(8-7-6-5-4-3-2-1) so they must be ingested, but they must never reach the models — see §11.

**`session_result.points` is authoritative.** Taking OpenF1's own award rather than deriving
points from finishing position means the sprint scale and any future rule change need no code
here. Only races that have not happened are scored from a table (`ChampionshipPoints`).

**`drivers` is populated for sessions that have not run yet**, which is what makes an entry
list — and therefore a preview — available before anyone has driven. It is also the only
source of driver names and teams anywhere in the system; everything else keys on car number.

Two caveats found in live data, both handled:

- OpenF1 has rounds it never published a result for (the 2026 Bahrain and Saudi Arabian
  Grands Prix among them). They stay unclassified for ever, so "still to run" means
  unclassified **and** in the future — otherwise they inflate the points still available.
- Free-tier coverage starts at 2023, and pre-season testing meetings have no race session.

Field names are snake_case throughout, so `OpenF1HttpClient` applies
`JsonNamingPolicy.SnakeCaseLower` rather than annotating every DTO property.

Politeness is enforced by `PolitenessDelayHandler`, a `DelegatingHandler` that spaces
requests ~500ms apart across all callers. Retries and 429 handling come from the standard
resilience pipeline, not hand-rolled loops.

## 11. Feature Engineering

All rules live on `DriverRaceFeature` in the Domain, so there is exactly one definition of
each. Per race session with both grid and result data:

- `polePace` = min `lap_duration` across the grid (drivers with no time excluded)
- `rainedInSession` = any weather row with `rainfall > 0`
- Per driver in `session_result` where `!Dns` and `Position` is not null:
  - `GridPosition` — their grid position, or `grid count + 1` for a pit lane start
  - `QualiGapToPole` — `lap_duration - polePace`, or the `99` sentinel if they set no time
  - `PitStopCount`, `AvgPitStopDuration` — from `stop_duration ?? lane_duration`
  - `Podium` = `Position <= 3 && !Dsq`; `PointsFinish` = `Position <= 10 && !Dsq`

`RebuildFeaturesCommand` clears and regenerates the whole table rather than updating it
incrementally — cheap at this volume, and it rules out stale-feature bugs.

**Grands Prix only.** Both `RebuildFeaturesCommandHandler` and `SeasonFeatureSet.LoadAsync`
filter `!IsSprint && IsClassified`. This is load-bearing, not tidiness: a sprint has both a
grid and results, so without the filter it silently becomes a training row — and a third of
the distance, an 8-point scale and usually no pit stop make "podium" and "points finish" mean
something else entirely there. If a feature rebuild ever reports a suspiciously high race
count, this filter is the first thing to check.

## 12. Model Training

Two independent binary classifiers over the same five features, differing only in label:

```
Concatenate("Features", GridPosition, QualiGapToPole, PitStopCount, AvgPitStopDuration, Rainfall)
  -> NormalizeMinMax("Features")
  -> SdcaLogisticRegression(labelColumnName: "Label")
```

80/20 split, seed 42 for reproducibility. Models are saved to the directory configured at
`MachineLearning:ModelDirectory` (default `models/`).

**Read AUC and F1, never accuracy.** Podium is ~15% of rows, so "always predict no" scores
~85% accuracy while being useless. `TrainModelsResponse` carries this warning in the payload
itself for that reason. AUC near 0.5 is noise; grid position alone should clear 0.65.

Not AutoML: ML.NET's AutoML API is still preview, and a working baseline should not depend
on a shifting surface. Swapping it in later means replacing pipeline construction only.

## 12a. Next-Race Preview

The classifiers need `GridPosition` and `QualiGapToPole`, which do not exist until qualifying
has run — which is most of the week. Rather than show nothing until Saturday,
`PreviewNextRaceQueryHandler` predicts either way and says which it did:

- **Grid published** → real `StartingGridEntry` rows, `gridConfirmed: true`.
- **Not yet** → a grid projected from recent form, `gridConfirmed: false`, and the UI labels it.

The projection (`StartingGridProjection.FromRecentForm`) is a *ranking* of each driver's
recency-weighted average gap to pole, not the averages themselves — the model was fitted on
grid slots 1..20 and would never have seen a field where six drivers start "4.3rd". Ranking on
gap rather than on average grid position keeps the two features rank-consistent, as they are in
a real session. Pit-stop inputs come from the driver's season average; rainfall is always 0,
because nothing here forecasts weather.

Feature rows are built in memory and handed straight to `IRacePredictor` — nothing requires
they be persisted. Sprints are never previewed, for the §11 reason.

## 12b. Championship Forecasting

Lives in `F1Predictor.Domain/Championship/`, entirely pure and dependency-free.

The SDCA classifiers cannot answer "who wins the title": they need a grid a future race does
not have, and two marginal probabilities do not describe a finishing order — and a championship
is decided by orders, not margins. So:

1. **`DriverFormModel`** fits a Plackett–Luce strength per driver from the season's Grand Prix
   finishing orders, by the standard MM iteration (Hunter, 2004). Three choices earn their
   place at ~14 races:
   - **Recency weighting**, half-life 5 races — a car developed over a season is not one car.
   - **Retirements censored**, not scored as last place. A blown engine says nothing about pace;
     reliability is modelled separately as a per-driver DNF rate shrunk toward the field mean.
   - **Top-10 partial ranking** rather than the full order. This one was found empirically: with
     the full order, a driver with six wins and one fifteenth place fitted *below* midfielders,
     because P(fifteenth) for a quick driver is so small that one bad race outweighs six wins.
     It also removes an asymmetry — censoring retirements protects a driver who crashes out
     while punishing one who limps home. Modelling only the points-paying positions fixes both.
2. **`ChampionshipSimulator`** plays the remaining calendar out 10,000 times, sampling each
   session's order via the Gumbel-max trick (one sort per session, not n sequential draws),
   dropping sampled retirements to the back with nothing, awarding real points, and settling
   both tables by count-back. Title probability is the share of seasons an entrant finished top.
3. **`TitleScenarioAnalyser`** answers the same question as arithmetic instead: who is
   mathematically alive, what the leader needs to clinch, and — assuming a contender wins out —
   the best average finish the leader could still manage and lose.

**The seed is fixed at 42, and that matters.** The dashboard re-reads the forecast on a timer;
an unseeded run would jitter the headline number by tenths on every poll while saying nothing.
`CachedForecast` keys on the latest classified session, so a new result invalidates it by
itself and the five-minute expiry is only a backstop.

`DeterministicRandom` is a hand-rolled xoshiro256++ rather than `System.Random` on purpose:
the framework makes no promise a given seed yields the same sequence across .NET releases, so
a shared seed alone would not keep published odds stable.

**Honest limits**: one season is a thin basis for a pace estimate, and nothing here knows about
upgrades, penalties, weather, or a driver's record at a given circuit. The
`ChampionshipForecastResponse.Method` field carries this caveat in the payload, in the same
spirit as `TrainModelsResponse.MetricGuidance`.

## 13. Endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/seasons/{year}/ingest?force=` | Ingest a season from OpenF1 (~4–5 min, idempotent) |
| GET | `/api/seasons/{year}/races` | List ingested sessions, flagged sprint/classified |
| POST | `/api/features/rebuild` | Regenerate the feature table |
| POST | `/api/models/train?year=` | Train both models, return metrics |
| GET | `/api/seasons/{year}/holdout` | Predictions vs. reality for the held-out race |
| GET | `/api/races/{sessionKey}/predictions` | Per-driver probabilities for one race |
| GET | `/api/seasons/{year}/next-race` | Preview the next Grand Prix (see §12a) |
| GET | `/api/seasons/{year}/standings` | Drivers' and constructors' tables |
| GET | `/api/seasons/{year}/championship-forecast` | Title odds for both championships (see §12b) |
| GET | `/api/seasons/{year}/title-scenarios?topN=` | What each contender needs |
| POST | `/api/admin/import-legacy-sqlite` | One-off SQLite import (**Development only**) |
| GET | `/health` | Health check |

`?force=true` on ingest re-fetches and replaces sessions already stored, instead of skipping
them. Without it a session is written exactly once and never revisited, so it is the only route
by which a new column gets backfilled or a provisional classification corrected. A session
stored as *scheduled* is always re-checked, so a race is picked up automatically once it runs.

## 14. Setup & Run

The connection string carries a password, so it belongs in user secrets, never in
`appsettings.json`:

```bash
dotnet user-secrets set "ConnectionStrings:LocalDb" "<your Neon connection string>" \
  --project F1Predictor.AppHost
```

Neon requires SSL — the string should include `SSL Mode=Require;Trust Server Certificate=true`.

```bash
dotnet build F1Predictor.slnx
aspire run
```

Migrations are applied automatically at startup in Development, so there is no separate
`dotnet ef database update` step. To add a migration after a model change:

```bash
dotnet ef migrations add <Name> --project F1Predictor.Infrastructure \
  --startup-project F1Predictor.WebApi --context ApplicationDbContext \
  --output-dir Database\Migrations
```

`--context` is required because the legacy SQLite import declares a second `DbContext`.

Then, from `/scalar`: ingest a season → rebuild features → train → read the holdout table.

`AppHost.cs` only ever runs the frontend locally, where `NEXT_PUBLIC_SITE_URL`'s
`http://localhost:3000` fallback (`F1Predictor.Web/src/app/layout.tsx`) is already correct.
Whatever hosts `F1Predictor.Web` in production must set `NEXT_PUBLIC_SITE_URL` itself to the
real public origin — see the comment in `F1Predictor.Web/.env.example`. It feeds
`metadataBase`, the OpenGraph image, `robots.ts`, and `sitemap.ts`, so a stale value quietly
breaks share previews and the sitemap's URLs, not just a cosmetic default.

## 15. Testing

No automated test project exists yet. `F1Predictor.slnx` has an empty `/tests/` solution
folder left over from the CleanArchitectureGenerator scaffold, and no project in the
solution references xUnit, NUnit, MSTest, Moq, or NSubstitute. When tests are added,
record the chosen framework and conventions here rather than assuming any.

## 16. Known Scope Cuts

- No tyre/strategy features (`stints` endpoint not ingested)
- Single season (~480 driver-race rows) — enough to prove the pipeline, not enough to trust
- `QualiGapToPole = 99` sentinel instead of proper imputation
- Ingestion runs synchronously inside the request; a background queue is a later concern
- `LegacySqliteImporter` and the SQLite package reference are temporary and should be
  deleted once the prototype's data is safely across

## 17. Next Steps

1. Ingest 2–3 seasons instead of one for a less anemic training set
2. Add the `stints` endpoint → tyre compound/strategy features
3. Try the AutoML upgrade path once the SDCA baseline is trusted
4. React frontend: race selector + predicted probability table
5. Move ingestion onto a background queue so the endpoint returns 202 immediately

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
