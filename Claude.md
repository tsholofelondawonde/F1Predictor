# F1 Race Predictor

## 1. Overview

A Clean Architecture .NET solution that predicts F1 race outcomes: it ingests historical
race data from OpenF1, engineers a small feature set, trains two binary classifiers
(podium probability, points-finish probability), and serves both training and predictions
over a minimal API.

This started as a flat console walking skeleton and has since been ported onto the layered
scaffold produced by [CleanArchitectureGenerator](https://www.nuget.org/packages/CleanArchitectureGenerator)
(`cleanarch new F1Predictor -d postgres --aspire`). The pipeline itself is unchanged — the
same features, the same SDCA classifiers, the same holdout check — but each part now lives
in the layer that owns it, and the console's `Console.WriteLine` reporting has become
`Result<T>`-returning use cases behind HTTP endpoints.

Still deliberately out of scope: a frontend, multi-season training, tyre-strategy features,
and any ranking model. Those remain the natural next steps.

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
| API docs | Scalar over OpenAPI, at `/scalar` |

There is no container runtime on the development machine, so Aspire references Neon as an
external connection string rather than provisioning a Postgres container.

## 3. Project Structure

```
F1Predictor/
├── SharedKernel/                   Entity base, Result<T>, Error, domain event contracts
├── F1Predictor.Domain/
│   ├── RaceData/Entities/          Meeting, RaceSession, StartingGridEntry,
│   │                               SessionResultEntry, PitStopEntry, WeatherReading
│   └── Predictions/                DriverRaceFeature + the feature engineering rules
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
├── F1Predictor.AppHost/            Aspire orchestration
└── F1Predictor.ServiceDefaults/    Aspire telemetry/health defaults
```

Dependencies point inward only. Domain references nothing but `SharedKernel`; Application
defines the ports; Infrastructure implements them; WebApi composes.

## 4. Data Source: OpenF1

Base URL `https://api.openf1.org/v1/`, no auth, **2023 onwards only** on the free tier.

| Endpoint | Query by | Key fields |
|---|---|---|
| `meetings` | `year` | `meeting_key`, `year`, `circuit_short_name`, `country_name`, `meeting_name`, `date_start` |
| `sessions` | `meeting_key` | `session_key`, `meeting_key`, `session_name`, `session_type`, `date_start` |
| `starting_grid` | **qualifying** `session_key` | `position`, `driver_number`, `lap_duration` |
| `session_result` | **race** `session_key` | `driver_number`, `position` (nullable), `dnf`, `dns`, `dsq` |
| `pit` | **race** `session_key` | `driver_number`, `stop_duration` (null before the 2024 US GP — falls back to `lane_duration`), `lane_duration` |
| `weather` | **race** `session_key` | `rainfall` (0/1), `track_temperature` |

**The grid comes from the qualifying session, not the race session.** This was verified
against live responses and contradicts what the original plan assumed, which is why
`RaceSession` carries a `QualifyingSessionKey` pointer.

Field names are snake_case throughout, so `OpenF1HttpClient` applies
`JsonNamingPolicy.SnakeCaseLower` rather than annotating every DTO property.

Politeness is enforced by `PolitenessDelayHandler`, a `DelegatingHandler` that spaces
requests ~500ms apart across all callers. Retries and 429 handling come from the standard
resilience pipeline, not hand-rolled loops.

## 5. Feature Engineering

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

## 6. Model Training

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

## 7. Endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/seasons/{year}/ingest` | Ingest a season from OpenF1 (~1–2 min, idempotent) |
| GET | `/api/seasons/{year}/races` | List ingested races with their session keys |
| POST | `/api/features/rebuild` | Regenerate the feature table |
| POST | `/api/models/train?year=` | Train both models, return metrics |
| GET | `/api/seasons/{year}/holdout` | Predictions vs. reality for the held-out race |
| GET | `/api/races/{sessionKey}/predictions` | Per-driver probabilities for one race |
| POST | `/api/admin/import-legacy-sqlite` | One-off SQLite import (**Development only**) |
| GET | `/health` | Health check |

## 8. Setup & Run

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

## 9. Known Scope Cuts

- No tyre/strategy features (`stints` endpoint not ingested)
- Single season (~480 driver-race rows) — enough to prove the pipeline, not enough to trust
- `QualiGapToPole = 99` sentinel instead of proper imputation
- Ingestion runs synchronously inside the request; a background queue is a later concern
- `LegacySqliteImporter` and the SQLite package reference are temporary and should be
  deleted once the prototype's data is safely across

## 10. Next Steps

1. Ingest 2–3 seasons instead of one for a less anemic training set
2. Add the `stints` endpoint → tyre compound/strategy features
3. Try the AutoML upgrade path once the SDCA baseline is trusted
4. React frontend: race selector + predicted probability table
5. Move ingestion onto a background queue so the endpoint returns 202 immediately
