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

A reference deployment hosts the frontend on Vercel: a project builds from the repo with
**Root Directory `F1Predictor.Web`** and carries `NEXT_PUBLIC_API_URL`,
`NEXT_PUBLIC_SITE_URL` and `NEXT_PUBLIC_API_KEY` as project environment variables
(Production + Preview). `next.config.ts` deliberately restates none of them — Next inlines
`NEXT_PUBLIC_*` at build time, so a value missing there is a dashboard fix and a redeploy,
not a code change.

## Deployment

> The pipeline below is the maintainer's reference deployment to Azure Container Apps.
> All resource names are placeholders (`<your-…>`) — a fork wires it to its own cloud by
> setting the GitHub Actions **repository variables** the workflow reads (`REGISTRY`,
> `REGISTRY_NAME`, `IMAGE_NAME`, `RESOURCE_GROUP`, `CONTAINER_APP`, `APP_URL`) plus the
> `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` secrets. The `deploy`
> job is also guarded by `github.repository ==` the upstream slug, so it never runs on a
> fork until that guard is changed.

The API deploys itself. `.github/workflows/ci.yml` builds `F1Predictor.slnx` on every push
and PR; `.github/workflows/deploy.yml`, on a push to `main`, builds
`F1Predictor.WebApi/Dockerfile` (context: the repo root), pushes it to
`<your-registry>.azurecr.io/<your-image>`, points `<your-container-app>` at the new image
and polls `/health` until it reports `Healthy`. `workflow_dispatch` runs the same path by
hand.

Four things about it are load-bearing:

- **The image is deployed by commit SHA, never `:latest`.** Re-pointing the app at the tag
  it already runs does not reliably create a revision, and a moving tag makes "which commit
  is live" unanswerable. `:latest` is pushed as a pointer only.
- **Azure is authenticated by OIDC federated credential**, not a stored client secret: an
  app registration trusts this repo's `main` branch and its `azure-production` environment,
  and holds only `AcrPush` on the registry plus `Contributor` on the container app. The
  registry's admin user is disabled, so nothing else would work anyway. The repo secrets
  `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` are identifiers, not
  credentials.

  **The trusted subjects use GitHub's immutable form** — the numeric owner and repo IDs are
  part of the string:

  ```
  repo:<owner>@<owner-id>/<repo>@<repo-id>:environment:azure-production
  repo:<owner>@<owner-id>/<repo>@<repo-id>:ref:refs/heads/main
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
- **The smoke test polls rather than curling once, and asserts the commit, not just
  `Healthy`.** The container app sits at `minReplicas: 0`, so the first request after a swap
  cold-starts a replica — hence the poll (`.github/scripts/wait-for-health.sh`, 20 × 15s).
  The commit check matters for a subtler reason: `az containerapp update` returns once the
  update is *accepted*, and if the new revision then fails to provision, the container app
  keeps the previous revision serving. `/health` answers `Healthy` — from the old code — and
  a status-only check would report that as a successful deploy of a commit that never ran.
  So the Dockerfile bakes `GIT_SHA` into `BUILD_SHA`, `/health` echoes it as `build`, and the
  poll requires both. The `ARG` sits in the final stage, after the publish `COPY`, so a new
  SHA every commit invalidates one layer rather than the whole build cache.
- **A failed smoke test rolls back, and verifies the rollback.** The app runs in Single
  revision mode, so the new revision already holds 100% of traffic by the time the poll
  starts: a failure is a live outage, not a pending one. The workflow records the running
  image *before* the update and re-deploys it on failure, refusing to act when there is no
  previous image or when it is the same SHA (a re-run would otherwise "roll back" to the
  failing build and report success). The rollback is then polled too — it creates a *third*
  revision from the old image, a cold start rather than a reactivation, so an environmental
  failure (Neon unreachable, a bad `Security__ApiKey`) reproduces on it, and the workflow says
  "the API is down" instead of "rolled back".

**Migrations are not part of the pipeline.** `Program.cs` applies them only in Development,
deliberately — the container app can scale out, and concurrent replicas racing
`Database.Migrate()` at startup is worse than migrating out of band. After a model change,
run `dotnet ef database update` against Neon by hand before merging the deploy.

Container-app configuration is not part of the pipeline either: `Security__ApiKey`,
`ConnectionStrings__ProdDb` and `Frontend__BaseUrl` are already set on the app, backed by
the `api-key` and `neon-connection-string` secrets. A deploy only ever swaps the image.

## Branch protection

`main` is protected by a repository **ruleset** named `protect-main`, checked in at
`.github/rulesets/protect-main.json`. Branch protection is not available on a private repo on
the Free plan, so this is applied **once, right after the repo is made public** (or after an
upgrade to GitHub Pro):

```bash
gh api --method POST repos/tsholofelondawonde/F1Predictor/rulesets \
  --input .github/rulesets/protect-main.json
```

Expect `201` and a ruleset id. Confirm with
`gh api repos/tsholofelondawonde/F1Predictor/rulesets`. To change the ruleset later, edit the
JSON and `PUT` it to `.../rulesets/<id>`.

What it enforces on `~DEFAULT_BRANCH`:

- **No force-push, no deletion** (`non_fast_forward`, `deletion`).
- **A pull request before merge**, with `required_approving_review_count: 0` — a solo
  maintainer can't approve their own PR, so this gates on CI rather than review. `CODEOWNERS`
  still auto-requests a review, but `require_code_owner_review` is off, so it doesn't block.
- **`Build & Verify` must pass** — the job name in `.github/workflows/ci.yml`, which runs on
  every PR. (`deploy.yml` also has a job of that name, but it only fires on push to `main`, so
  on a PR the check resolves to the CI run.) `strict_required_status_checks_policy: true` also
  requires the PR branch to be up to date with `main` before it can merge.
- **Admins bypass** (`bypass_actors`, `bypass_mode: always`) so a hotfix can still go straight
  to `main`. `actor_id: 5` is the built-in Admin role — verify against
  `gh api repos/tsholofelondawonde/F1Predictor/rulesets/rule-suites` on first run and adjust
  if GitHub reports a different id. Drop the `bypass_actors` entry to enforce on everyone.

Dependabot PRs gate on `Build & Verify` like any other PR; merge them once green (or enable
auto-merge separately).
