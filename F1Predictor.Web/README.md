# F1Predictor.Web (GridMind)

The Next.js 16 App Router front end for [F1Predictor](../README.md). Talks to the minimal
API over `NEXT_PUBLIC_API_URL`.

## Heads up: Next.js 16

This is **not** the Next.js most references (or a model's training data) describe. `params`
and `searchParams` are Promises, pages are typed with the generated `PageProps<"/route">`
helper, and several conventions changed. Read the guides under
`node_modules/next/dist/docs/` before writing route or layout code — see [`AGENTS.md`](AGENTS.md).

## Run

Normally you don't run this directly — `aspire run` from the repo root starts it and injects
`NEXT_PUBLIC_API_URL`. Standalone:

```bash
cp .env.example .env.local   # then fill in the values
npm install
npm run dev                  # http://localhost:3000
```

## Environment

| Variable | Purpose |
|---|---|
| `NEXT_PUBLIC_API_URL` | Base URL of the F1Predictor API. |
| `NEXT_PUBLIC_SITE_URL` | Public origin of this site — feeds `metadataBase`, OG images, `robots.ts`, `sitemap.ts`. Must be the real origin in production, never `localhost`. |
| `NEXT_PUBLIC_API_KEY` | Must match `Security:ApiKey` on the API. Sent as `X-Api-Key`. A low bar, not access control — it ships in this public bundle. |

All three are inlined by Next at **build time**, so changing one in a hosting dashboard needs
a redeploy, not just a restart.

## Structure

```
src/
├── app/         Routes, layouts, metadata, OG images
├── features/    Feature slices (landing, ...)
└── shared/      Shared components, API client, stores
```

## Checks

```bash
npm run lint
npm run build
```
