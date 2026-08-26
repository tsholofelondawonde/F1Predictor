# Setup & Run

The connection string carries a password, so it belongs in user secrets, never in
`appsettings.json`:

```bash
dotnet user-secrets set "ConnectionStrings:LocalDb" "<your Neon connection string>" \
  --project F1Predictor.AppHost
```

Neon requires SSL — the string should include `SSL Mode=Require;Trust Server Certificate=true`.

`POST /api/seasons/{year}/ingest`, `/api/features/rebuild`, `/api/models/train`, and
`/api/admin/import-legacy-sqlite` all require an `X-Api-Key` header matching
`Security:ApiKey` — set via user secrets, never `appsettings.json`:

```bash
dotnet user-secrets set "Security:ApiKey" "<a random value>" --project F1Predictor.WebApi
```

The frontend needs the same value in `F1Predictor.Web/.env.local` as `NEXT_PUBLIC_API_KEY`
so its own Ingest/Rebuild/Train buttons keep working (see `.env.example`). This is a low
bar, not real access control — the key ships in the public frontend bundle, so it only
filters out naive automated hits against the raw API, not a determined attacker. Fails
closed: with no key configured, the four routes reject every request.

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
