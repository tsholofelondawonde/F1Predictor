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

In production that host is Vercel: the `f1predictor` project builds from the repo with
**Root Directory `F1Predictor.Web`** and carries `NEXT_PUBLIC_API_URL`,
`NEXT_PUBLIC_SITE_URL` and `NEXT_PUBLIC_API_KEY` as project environment variables
(Production + Preview). `next.config.ts` deliberately restates none of them — Next inlines
`NEXT_PUBLIC_*` at build time, so a value missing there is a dashboard fix and a redeploy,
not a code change.

## Deployment

The API deploys itself. `.github/workflows/build.yml` builds `F1Predictor.slnx`, and on a
push to `main` a second job builds `F1Predictor.WebApi/Dockerfile` (context: the repo root),
pushes it to `crgridminddev001.azurecr.io/grid-mind-api`, points
`ca-grid-mind-api-dev-001` at the new image and polls `/health` until it reports `Healthy`.
`workflow_dispatch` runs the same path by hand.

Three things about it are load-bearing:

- **The image is deployed by commit SHA, never `:latest`.** Re-pointing the app at the tag
  it already runs does not reliably create a revision, and a moving tag makes "which commit
  is live" unanswerable. `:latest` is pushed as a pointer only.
- **Azure is authenticated by OIDC federated credential**, not a stored client secret: the
  app registration `gh-actions-f1predictor` trusts this repo's `main` branch and its
  `azure-production` environment, and holds only `AcrPush` on the registry plus
  `Contributor` on the container app. The registry's admin user is disabled, so nothing
  else would work anyway. The repo secrets `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` /
  `AZURE_SUBSCRIPTION_ID` are identifiers, not credentials.

  **The trusted subjects use GitHub's immutable form** — the numeric owner and repo IDs are
  part of the string:

  ```
  repo:tsholofelondawonde@89080883/F1Predictor@1326662270:environment:azure-production
  repo:tsholofelondawonde@89080883/F1Predictor@1326662270:ref:refs/heads/main
  ```

  Write a credential with the older `repo:<owner>/<repo>:...` form and login fails with
  `AADSTS700213: No matching federated identity record found`, which is exactly how the
  deploy job failed the first time it ran. GitHub issues the sub claim this way regardless of
  the repository's OIDC customization setting — `GET /repos/{owner}/{repo}/actions/oidc/
  customization/sub` reports `use_default: true` while already returning the immutable
  `sub_claim_prefix`, so there is nothing to switch off. It is also the better target: the
  numeric IDs survive a repo or account rename. Because the deploy job declares
  `environment: azure-production`, the `environment:` subject is the one presented; the
  `ref:` one only matters if that block is ever removed.
- **The smoke test polls rather than curling once.** The container app sits at
  `minReplicas: 0`, so the first request after a swap cold-starts a replica.

**Migrations are not part of the pipeline.** `Program.cs` applies them only in Development,
deliberately — the container app can scale out, and concurrent replicas racing
`Database.Migrate()` at startup is worse than migrating out of band. After a model change,
run `dotnet ef database update` against Neon by hand before merging the deploy.

Container-app configuration is not part of the pipeline either: `Security__ApiKey`,
`ConnectionStrings__ProdDb` and `Frontend__BaseUrl` are already set on the app, backed by
the `api-key` and `neon-connection-string` secrets. A deploy only ever swaps the image.
