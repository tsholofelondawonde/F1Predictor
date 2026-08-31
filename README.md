# F1Predictor · GridMind

[![CI](https://github.com/tsholofelondawonde/F1Predictor/actions/workflows/ci.yml/badge.svg)](https://github.com/tsholofelondawonde/F1Predictor/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)

Predicts Formula 1 race outcomes from [OpenF1](https://openf1.org) data, and looks forward:
next-Grand-Prix previews, live championship tables, and a Monte Carlo season simulator for
title odds.

- Two **ML.NET** binary classifiers — podium probability and points-finish probability —
  trained on a small engineered per-driver feature set (grid position, gap to pole, pit-stop
  count and duration, rainfall).
- A **next-race preview** that works before qualifying by projecting a grid from recent form,
  and says which it did.
- A hand-rolled **championship forecaster** in the domain: a Plackett–Luce strength model
  fitted from the season's finishing orders, played out over the remaining calendar 10,000
  times, plus an arithmetic "who's mathematically alive / what the leader needs to clinch"
  analyser.
- A **Next.js 16** front end (branded *GridMind*) over a minimal API.

> **Naming:** the solution, namespaces and this repository are **F1Predictor**; the
> user-facing web app is branded **GridMind**. Both are intentional.

> **Status:** a working prototype. It trains on a single season (~480 driver-race rows) —
> enough to prove the pipeline end to end, not enough for the predictions to be taken as
> authoritative. See [`.claude/rules/project-status.md`](.claude/rules/project-status.md).

## Architecture

Clean Architecture, dependencies pointing inward only.

```
F1Predictor/
├── SharedKernel/                 Entity base, Result<T>, Error, domain event contracts
├── F1Predictor.Domain/
│   ├── RaceData/                 Meeting, RaceSession, grid / result / pit / weather entities
│   ├── Predictions/              DriverRaceFeature + the feature engineering rules
│   └── Championship/             Points scale, standings + count-back, Plackett–Luce fit,
│                                 Monte Carlo simulator, title scenarios
├── F1Predictor.Application/      Use cases (one folder per command/query), port interfaces
├── F1Predictor.Infrastructure/   EF Core + Npgsql, OpenF1 HTTP client, ML.NET trainer/predictor
├── F1Predictor.WebApi/           One sealed class per endpoint, discovered by assembly scan
├── F1Predictor.Web/              Next.js front end (run by the AppHost, not in the .slnx)
├── F1Predictor.AppHost/          .NET Aspire orchestration
└── F1Predictor.ServiceDefaults/  Aspire telemetry / health defaults
```

| Concern | Choice |
|---|---|
| Framework | .NET 10, minimal APIs |
| Orchestration | .NET Aspire |
| Database | PostgreSQL (EF Core 10 + Npgsql) |
| CQRS | Scaffold's own handler interfaces — no MediatR |
| Validation | FluentValidation via a handler decorator |
| ML | ML.NET — `SdcaLogisticRegression` |
| Championship odds | Plackett–Luce + Monte Carlo, hand-rolled in the Domain |
| API docs | Scalar over OpenAPI, at `/scalar` |
| Frontend | Next.js 16 App Router, React 19, Tailwind 4 |

Deeper notes live under [`.claude/rules/`](.claude/rules/) —
[architecture](.claude/rules/architecture.md),
[conventions](.claude/rules/conventions.md),
[ML pipeline](.claude/rules/ml-pipeline.md),
[API](.claude/rules/api-conventions.md).

## Quick start

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download), Node.js 20+, the Aspire
workload (`dotnet workload install aspire`), and a PostgreSQL database (a free
[Neon](https://neon.tech) project works, or local Postgres).

```bash
# secrets — never committed
dotnet user-secrets set "ConnectionStrings:LocalDb" "<your Postgres connection string>" \
  --project F1Predictor.AppHost
dotnet user-secrets set "Security:ApiKey" "<any random value>" --project F1Predictor.WebApi

# frontend env
cp F1Predictor.Web/.env.example F1Predictor.Web/.env.local
# then set NEXT_PUBLIC_API_KEY in .env.local to the same value as Security:ApiKey

dotnet build F1Predictor.slnx
aspire run
```

For Neon, the connection string needs `SSL Mode=Require;Trust Server Certificate=true`.
Migrations apply automatically at startup in Development.

Then open the API's `/scalar` UI and run the pipeline in order:

1. `POST /api/seasons/{year}/ingest` — pull a season from OpenF1 (~4–5 min, idempotent)
2. `POST /api/features/rebuild` — regenerate the feature table
3. `POST /api/models/train?year=` — train both classifiers, return metrics
4. `GET /api/seasons/{year}/holdout` — predictions vs. reality for the held-out race

The ingest / rebuild / train routes require an `X-Api-Key` header — see
[`SECURITY.md`](SECURITY.md) for what that key is and isn't.

Full API table: [`.claude/rules/api-conventions.md`](.claude/rules/api-conventions.md).

## Contributing

Issues and PRs welcome — see [`CONTRIBUTING.md`](CONTRIBUTING.md) and
[`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md).

## Credits

- Race data: the [OpenF1 API](https://openf1.org) (2023 onwards on the free tier). Please
  read and respect OpenF1's terms if you build on this.
- [ML.NET](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet) for the classifiers.
- Layered scaffold from
  [CleanArchitectureGenerator](https://www.nuget.org/packages/CleanArchitectureGenerator).
- Plackett–Luce fit follows the standard MM iteration (Hunter, 2004).

## License

[MIT](LICENSE).
