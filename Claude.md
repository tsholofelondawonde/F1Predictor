# F1 Race Predictor — Implementation Plan

## 1. Overview

A single, self-contained .NET console app that proves out a predictive ML pipeline for
F1 race outcomes: ingest historical race data from OpenF1, engineer a small feature set,
train two binary classifiers (podium probability, points-finish probability), and print
predictions for a held-out race to sanity-check the whole thing end to end.

**This is a walking skeleton, not the production build.** No deadline, exploratory
project — the goal is proving the pipeline works before making any part of it clever.
Explicitly not in scope for this pass: Clean Architecture layering, a web API, a
frontend, multi-season training, or tyre-strategy features. Those are the natural v2s
once this runs and the holdout predictions look sane.

Flat project structure on purpose — one console app, no DI container, no repository
abstraction. Don't introduce that ceremony until there's a second consumer (an API) that
actually needs it.

## 2. Tech Stack & Packages

- .NET console app (match whatever SDK `dotnet --version` reports locally — net10.0
  assumed, drop to net9.0/net8.0 in the `.csproj` if needed)
- `Microsoft.EntityFrameworkCore.Sqlite` — local file DB, zero external setup
- `Microsoft.ML` — training (stable `SdcaLogisticRegression`, not the AutoML preview API)
- `System.Net.Http.Json` — typed OpenF1 client

```bash
dotnet new console -n F1Predictor
cd F1Predictor
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.ML
dotnet add package System.Net.Http.Json
```

## 3. Project Structure

```
F1Predictor/
  F1Predictor.csproj
  Program.cs                       <- orchestrates the full pipeline, top to bottom
  OpenF1/
    Dtos.cs                        <- MeetingDto, SessionDto, StartingGridDto,
                                       SessionResultDto, PitDto, WeatherDto
    OpenF1Client.cs                <- typed HttpClient wrapper
  Data/
    Entities.cs                    <- raw + engineered EF Core entities
    AppDbContext.cs                <- SQLite context
  Ingestion/
    SeasonIngestor.cs              <- meetings -> race sessions -> raw table rows
  Features/
    FeatureBuilder.cs              <- raw tables -> DriverRaceFeature rows
  MachineLearning/
    RaceFeatureInput.cs / RacePredictionOutput.cs
    ModelTrainer.cs                <- train, evaluate, save, predict
```

## 4. Data Source: OpenF1 API

Base URL: `https://api.openf1.org/v1/`. No auth needed for historical data. Free tier
covers **2023 onwards only** — pick a completed season (2023, 2024, 2025) for training.
Add a ~200ms delay between calls; it's a free, community-run API, be a polite citizen
rather than a load test.

Endpoints used, and the exact JSON field names to map:

| Endpoint | Query by | Key fields |
|---|---|---|
| `meetings` | `year` | `meeting_key`, `year`, `circuit_short_name`, `country_name`, `meeting_name`, `date_start` |
| `sessions` | `meeting_key` | `session_key`, `meeting_key`, `session_name`, `session_type`, `date_start` |
| `starting_grid` | **race** `session_key` | `position`, `driver_number`, `lap_duration` |
| `session_result` | **race** `session_key` | `driver_number`, `position` (nullable), `dnf`, `dns`, `dsq` |
| `pit` | **race** `session_key` | `driver_number`, `stop_duration` (null before 2024 US GP — fall back to `lane_duration`), `lane_duration` |
| `weather` | **race** `session_key` | `rainfall` (0/1), `track_temperature` |

**Important:** `starting_grid` is queried using the **race** session's `session_key`, not
qualifying's — OpenF1 returns the grid for "the upcoming race" when you pass the race
session key. There's no need to separately discover/ingest the qualifying session at all
for this feature set.

Filter `sessions` results to `session_type == "Race"` to find the one session per meeting
you actually need.

## 5. Data Model

Raw tables (one row per API result, straight persistence, no transformation):

- `Meeting` — PK `MeetingKey`; `Year`, `CircuitShortName`, `CountryName`, `MeetingName`, `DateStart`
- `RaceSession` — PK `SessionKey`; `MeetingKey`, `SessionName`, `SessionType`, `DateStart`
- `StartingGridEntry` — `SessionKey`, `DriverNumber`, `Position`, `LapDuration` (nullable)
- `SessionResultEntry` — `SessionKey`, `DriverNumber`, `Position` (nullable), `Dnf`, `Dns`, `Dsq`
- `PitStopEntry` — `SessionKey`, `DriverNumber`, `StopDuration` (nullable), `LaneDuration` (nullable)
- `WeatherReading` — `SessionKey`, `Rainfall`, `TrackTemperature`

Engineered table (model-ready, one row per driver per race):

- `DriverRaceFeature` — `SessionKey`, `DriverNumber`, `GridPosition`, `QualiGapToPole`,
  `PitStopCount`, `AvgPitStopDuration`, `Rainfall`, `FinishPosition`, `Podium` (bool),
  `PointsFinish` (bool)

EF Core notes: `Meeting`/`RaceSession` need explicit `HasKey` config since their PKs
aren't named `Id`. Everything else uses the standard `Id` auto-increment convention.
`OnConfiguring` points at a local SQLite file (`f1predictor.db`); use
`Database.EnsureCreatedAsync()` at startup — no migrations needed for a throwaway proof
of concept.

## 6. Ingestion Flow

```
IngestSeasonAsync(year):
  meetings = GET meetings?year={year}
  for each meeting:
    upsert Meeting row
    sessions = GET sessions?meeting_key={meeting.meeting_key}
    raceSession = sessions.First(s => s.session_type == "Race")
    if raceSession is null: skip (weekend hasn't happened yet)
    if raceSession already ingested: skip (idempotent re-runs)

    grid    = GET starting_grid?session_key={raceSession.session_key}
    results = GET session_result?session_key={raceSession.session_key}
    pits    = GET pit?session_key={raceSession.session_key}
    weather = GET weather?session_key={raceSession.session_key}

    if results is empty: skip (race not yet classified)

    persist RaceSession, all StartingGridEntry/SessionResultEntry/PitStopEntry/WeatherReading rows
    delay 200ms between each of the four calls above
```

Re-running ingestion for the same year is safe — already-ingested meetings/sessions are
skipped rather than duplicated.

## 7. Feature Engineering

For each race session with both grid and result data:

- `polePace` = min `lap_duration` across the grid (drivers with no lap time excluded)
- `rainedInSession` = 1 if any weather row for the session has `rainfall > 0`, else 0
- For each driver in `session_result` where `!Dns` and `Position` is not null:
  - `GridPosition` = their grid `Position`, or `(grid count + 1)` if no grid entry (e.g. pit lane start)
  - `QualiGapToPole` = their `lap_duration - polePace`, or `99` as a sentinel if they set no time (crash in Q1, grid penalty, etc. — not a proper imputation, flagged as a known rough edge)
  - `PitStopCount` = count of their `pit` rows in the session
  - `AvgPitStopDuration` = average of `stop_duration ?? lane_duration` across their stops, or `0` if none
  - `Rainfall` = `rainedInSession`
  - `FinishPosition` = their result `Position`
  - `Podium` = `Position <= 3 && !Dsq`
  - `PointsFinish` = `Position <= 10 && !Dsq`

Rebuild the whole feature table from raw data each time (`FeatureBuilder` clears and
regenerates) rather than incrementally updating it — cheap at this data volume, and
avoids a whole class of "stale feature" bugs while the logic is still being tuned.

## 8. Model Training

Two independent binary classifiers, same feature set, different labels: podium and
points-finish. Not a ranking model — deliberately scoped down, see prior discussion on
why ranking is a v2+ problem.

**Primary approach — hand-written pipeline, not AutoML:**

```
pipeline =
  Concatenate("Features", GridPosition, QualiGapToPole, PitStopCount, AvgPitStopDuration, Rainfall)
  -> NormalizeMinMax("Features")
  -> SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features")
```

- 80/20 train/test split, seeded for reproducibility
- Evaluate with `BinaryClassification.Evaluate` — report **AUC and F1, not raw accuracy**.
  Podium is ~15% of rows, so "always predict no" already scores ~85% accuracy while being
  useless. This matters enough to bear repeating in the code's own console output.
- Save each trained model via `mlContext.Model.Save(model, schema, "podium-model.zip")` /
  `"points-model.zip"`

**Why not AutoML for v1:** Microsoft's own docs mark the ML.NET AutoML API "preview" —
don't gate a first working run on an API surface that might have shifted between package
versions. Get the guaranteed-stable SDCA pipeline running first.

**Optional upgrade path (v2):** once SDCA runs cleanly, add
`dotnet add package Microsoft.ML.AutoML` and let `mlContext.Auto()` sweep trainers and
hyperparameters instead of hand-picking SDCA. Same train/test split and evaluation
approach, swap the pipeline construction only.

## 9. Prediction / Holdout Validation

Before training, pick the **single most recently run race** in the ingested season and
exclude it entirely from the training set. After training both models, run predictions
for every driver in that held-out race and print a table: driver number, grid position,
actual finish position, whether they actually podiumed, and both models' predicted
probabilities. This is the "did this thing learn anything real" check — no formal
backtesting harness needed yet, just eyeball it.

## 10. Execution Flow (`Program.cs`)

Single top-to-bottom run, no subcommands — `dotnet run -- 2024`:

1. Ingest season → raw tables in SQLite
2. Build features → `DriverRaceFeature` table
3. Bail out early with a clear message if fewer than 3 races have usable feature data
   (season not yet run, or a bad year argument)
4. Identify holdout race (most recent by `date_start`), exclude from training set
5. Train podium model, print metrics
6. Train points model, print metrics
7. Predict holdout race, print comparison table
8. Print final summary (model file paths, DB file path)

## 11. Build Order

1. **Scaffold** — project + package references, confirm `dotnet build` succeeds with
   empty `Program.cs`
2. **OpenF1 client** — DTOs + client methods, manually test against one known
   `session_key` before writing any orchestration; confirm deserialization actually works
   against live responses
3. **Ingestion** — raw table persistence only, no features yet; run against one season,
   confirm row counts look sane (~20 drivers × ~24 races)
4. **Feature builder** — dumb features first (grid position, finish position only) to
   prove the shape works, then add pit/weather features
5. **Training** — SDCA pipeline, confirm it trains and produces non-garbage AUC (>0.5,
   ideally >0.65 given grid position alone is a decent podium predictor)
6. **Holdout prediction** — wire up the final comparison table

Don't skip ahead to step 6 before step 5 produces sane metrics — a model with AUC ~0.5 is
noise, and a nice-looking prediction table built on it is worse than useless, it's
actively misleading.

## 12. Known V1 Scope Cuts

- No tyre/strategy features (`stints` endpoint not ingested) — grid position and pit
  stops only
- Single season (~480 driver-race rows) — enough to prove the concept, not enough to
  trust the model's opinion about anything
- `QualiGapToPole = 99` sentinel instead of proper imputation for missing lap times
- No repository/DI abstraction — fine for a console app, not what you'd want once an API
  sits on top
- No multi-season blending, no track-specific or constructor-form features

## 13. Next Steps (v2 candidates, in rough priority order)

1. Confirm the pipeline runs and holdout predictions look directionally sane
2. Ingest 2-3 seasons instead of one for a less anemic training set
3. Add `stints` endpoint → tyre compound/strategy features
4. Try the AutoML upgrade path once SDCA baseline is trusted
5. Port into Clean Architecture layers (Domain/Application/Infrastructure/Api) with
   `PredictionEnginePool` serving the saved `.zip` models over an endpoint
6. React frontend: race selector + predicted probability table

## 14. Setup & Run

```bash
cd F1Predictor
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.ML
dotnet add package System.Net.Http.Json
dotnet build
dotnet run -- 2024
```

`f1predictor.db` (SQLite) and both `.zip` models land in the working directory.
Re-running with the same year is safe — ingestion skips already-fetched meetings.