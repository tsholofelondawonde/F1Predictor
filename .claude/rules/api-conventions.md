# API Conventions

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/seasons/{year}/ingest?force=` | Ingest a season from OpenF1 (~4–5 min, idempotent) |
| GET | `/api/seasons/{year}/races` | List ingested sessions, flagged sprint/classified |
| POST | `/api/features/rebuild` | Regenerate the feature table |
| POST | `/api/models/train?year=&fromYear=&notes=` | Train both models on seasons `fromYear..year`, record the run, return validation/baseline/holdout metrics |
| GET | `/api/models/runs?target=&take=` | Training-run log, newest first (see `ml-pipeline.md`) |
| GET | `/api/seasons/{year}/holdout` | Predictions vs. reality for the held-out race, with `modelWarning` |
| GET | `/api/seasons/{year}/data-status` | Whether ingest/features/models have fallen behind |
| GET | `/api/races/{sessionKey}/predictions` | Per-driver probabilities for one race |
| GET | `/api/seasons/{year}/next-race` | Preview the next Grand Prix (see `ml-pipeline.md`) |
| GET | `/api/seasons/{year}/standings` | Drivers' and constructors' tables |
| GET | `/api/seasons/{year}/championship-forecast` | Title odds for both championships (see `ml-pipeline.md`) |
| GET | `/api/seasons/{year}/title-scenarios?topN=` | What each contender needs |
| POST | `/api/admin/import-legacy-sqlite` | One-off SQLite import (**Development only**) |
| GET | `/api/ai/status` | Whether an AI provider is configured |
| GET | `/api/seasons/{year}/next-race/drivers/{driverNumber}/explanation` | Per-feature contributions (+ narrative when AI is on) |
| GET | `/api/races/{sessionKey}/preview` | Stored AI preview, with `stale` |
| POST | `/api/races/{sessionKey}/preview` | Generate/replace the preview (X-Api-Key, Mutating limit) |
| POST | `/api/ai/ask` | Analyst chat, `text/event-stream` (X-Api-Key, Analyst limit 10/min) |
| GET | `/api/races/{sessionKey}/predictions/{driverNumber}/explanation` | Contributions for a classified race, vs. what happened |
| GET | `/api/races/{sessionKey}/similar` | Most similar indexed races (pgvector; needs embeddings) |
| GET | `/api/analysis/search?q=` | Semantic search over race fact sheets (needs embeddings) |
| POST | `/api/admin/analysis/refresh` | Regenerate stale previews / index races on demand (X-Api-Key) |
| GET | `/health` | Health check |

The AI routes above are **paused** while `Ai:Enabled=false` (the current `appsettings.json`).
They stay mapped but fail closed with `Analysis.AiUnavailable`, or return no narrative.

`?force=true` on ingest re-fetches and replaces sessions already stored, instead of skipping
them. Without it a session is written exactly once and never revisited, so it is the only route
by which a new column gets backfilled or a provisional classification corrected. A session
stored as *scheduled* is always re-checked, so a race is picked up automatically once it runs.

`POST /api/ai/ask` sits behind its own `Analyst` rate-limit policy — 10 requests per minute per
IP, separate from the `Mutating` policy the rebuild/train/import/preview routes share (ingest
has its own `Ingest` policy) — because one analyst
question is a single LLM round trip that can itself make up to `MaxToolIterations` (6) tool calls,
which is a heavier unit of work than a plain mutating request. It streams its answer as
`text/event-stream` rather than a JSON body: the event vocabulary is `status` (a tool call is in
flight), `delta` (a text fragment), `done` (normal end of stream), and `error` (a transport fault —
logged server-side, reported to the client as a generic message, never the underlying exception).
