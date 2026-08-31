# Contributing

Thanks for your interest in F1Predictor / GridMind. This is a small project — issues and
pull requests are both welcome.

## Ground rules

- Open an issue before a large change so we can agree on the approach.
- Keep PRs focused. One concern per PR.
- Be respectful — see [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md).

## Getting set up

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 20+ (for `F1Predictor.Web`)
- The Aspire workload: `dotnet workload install aspire`
- A PostgreSQL database — a free [Neon](https://neon.tech) project works well, or local Postgres.

Configure secrets (never commit these — see [`SECURITY.md`](SECURITY.md)):

```bash
dotnet user-secrets set "ConnectionStrings:LocalDb" "<your Postgres connection string>" \
  --project F1Predictor.AppHost
dotnet user-secrets set "Security:ApiKey" "<any random value>" --project F1Predictor.WebApi
```

For Neon, include `SSL Mode=Require;Trust Server Certificate=true` in the connection string.

Frontend env — copy `F1Predictor.Web/.env.example` to `F1Predictor.Web/.env.local` and set
`NEXT_PUBLIC_API_KEY` to the same value you used above.

Run everything:

```bash
dotnet build F1Predictor.slnx
aspire run
```

Then from `/scalar`: ingest a season → rebuild features → train → read the holdout table.

More detail lives in [`.claude/rules/setup.md`](.claude/rules/setup.md).

## Conventions

Code style and architecture rules are documented under `.claude/rules/` — read
[`conventions.md`](.claude/rules/conventions.md) and
[`architecture.md`](.claude/rules/architecture.md) before a first PR. In short:

- Clean Architecture; dependencies point inward only.
- CQRS via the scaffold's own handler interfaces — no MediatR.
- `Result<T>` from every handler; exceptions only for genuine programmer errors.
- File-scoped namespaces, `sealed` implementations, `internal` by default.

## Before you push

- `dotnet build F1Predictor.slnx` must pass. **Warnings are errors** in this repo
  (`TreatWarningsAsErrors`, analysis level `latest`, Meziantou / Sonar / Roslynator analyzers).
- `cd F1Predictor.Web && npm run lint` for frontend changes.
- If you changed an EF model, add a migration (see `setup.md`) — but do **not** run
  migrations against a shared database as part of the PR.
- There is no test project yet. If you add one, note the framework in
  `.claude/rules/project-status.md`.

## Data source

All race data comes from the [OpenF1 API](https://openf1.org). Be considerate of it —
`PolitenessDelayHandler` already spaces requests; don't remove it.
