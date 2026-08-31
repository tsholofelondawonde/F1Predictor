# Architecture

## Overview

A Clean Architecture .NET solution that predicts F1 race outcomes: it ingests historical
race data from OpenF1, engineers a small feature set, trains two binary classifiers
(podium probability, points-finish probability), and serves both training and predictions
over a minimal API, with a Next.js frontend on top.

It also looks forwards, not just back: it previews the next Grand Prix, keeps both
championship tables, and simulates the rest of the season to give each driver and
constructor a title probability — see `ml-pipeline.md`.

This started as a flat console walking skeleton and has since been ported onto the layered
scaffold produced by [CleanArchitectureGenerator](https://www.nuget.org/packages/CleanArchitectureGenerator)
(`cleanarch new F1Predictor -d postgres --aspire`). The pipeline itself is unchanged — the
same features, the same SDCA classifiers, the same holdout check — but each part now lives
in the layer that owns it, and the console's `Console.WriteLine` reporting has become
`Result<T>`-returning use cases behind HTTP endpoints.

Still deliberately out of scope: multi-season training, tyre-strategy features, and any
ranking model over race results. Those remain the natural next steps.

## Tech Stack

| Concern | Choice |
|---|---|
| Framework | .NET 10, minimal APIs |
| Orchestration | .NET Aspire (`F1Predictor.AppHost`) |
| Database | PostgreSQL, hosted on [Neon](https://neon.tech) |
| ORM | EF Core 10 + Npgsql |
| CQRS | Scaffold's own `ICommandHandler<T,R>` / `IQueryHandler<T,R>` (no MediatR) |
| Validation | FluentValidation, applied by a handler decorator |
| ML | ML.NET 5 — `SdcaLogisticRegression`, not AutoML |
| Championship odds | Plackett–Luce + Monte Carlo, hand-rolled in the Domain (see `ml-pipeline.md`) |
| API docs | Scalar over OpenAPI, at `/scalar` |
| Frontend | Next.js 16 App Router, React 19, Tailwind 4, axios, zustand |

There is no container runtime on the development machine, so Aspire references Neon as an
external connection string rather than provisioning a Postgres container.

## Project Structure

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

## Architecture

- Clean Architecture, dependencies point inward only (Domain → Application →
  Infrastructure/WebApi — see Project Structure above).
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
