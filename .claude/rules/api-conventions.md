# API Conventions

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/seasons/{year}/ingest?force=` | Ingest a season from OpenF1 (~4–5 min, idempotent) |
| GET | `/api/seasons/{year}/races` | List ingested sessions, flagged sprint/classified |
| POST | `/api/features/rebuild` | Regenerate the feature table |
| POST | `/api/models/train?year=` | Train both models, return metrics |
| GET | `/api/seasons/{year}/holdout` | Predictions vs. reality for the held-out race |
| GET | `/api/races/{sessionKey}/predictions` | Per-driver probabilities for one race |
| GET | `/api/seasons/{year}/next-race` | Preview the next Grand Prix (see `ml-pipeline.md`) |
| GET | `/api/seasons/{year}/standings` | Drivers' and constructors' tables |
| GET | `/api/seasons/{year}/championship-forecast` | Title odds for both championships (see `ml-pipeline.md`) |
| GET | `/api/seasons/{year}/title-scenarios?topN=` | What each contender needs |
| POST | `/api/admin/import-legacy-sqlite` | One-off SQLite import (**Development only**) |
| GET | `/health` | Health check |

`?force=true` on ingest re-fetches and replaces sessions already stored, instead of skipping
them. Without it a session is written exactly once and never revisited, so it is the only route
by which a new column gets backfilled or a provisional classification corrected. A session
stored as *scheduled* is always re-checked, so a race is picked up automatically once it runs.
