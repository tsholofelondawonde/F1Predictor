# Project Status

## Testing

Two test projects live under `/tests/`: `F1Predictor.Application.Tests` (138 tests) and
`F1Predictor.Infrastructure.Tests` (30 tests), both xunit 2.9.3 + FluentAssertions 8.9.0. Test
methods are named `Method_Scenario_Expectation`. Fakes are hand-written in preference to Moq —
Moq is pinned in `Directory.Packages.props` for the rare case a hand-written fake isn't worth
it (the Quartz `IScheduler` in `SeasonIngestionCoordinatorJobTests` is one). `F1Predictor.Application.Tests`
keeps its fakes under a `Fakes/` folder (`FakeChatClient`, `FakeRacePredictor`, `FakeModelTrainer`,
`FakeAiCapabilities`, `InMemoryDb`, `SeasonSeed`, and so on); `F1Predictor.Infrastructure.Tests`
keeps its fakes next to the tests that use them (`MachineLearning/SyntheticModels.cs`,
`Ingestion/FakeJobExecutionContext.cs`).

The ML evaluation harness is covered in `F1Predictor.Application.Tests/Predictions/`:
`RaceTimeSplitTests`, `BinaryMetricsTests`, `TrainModelsCommandHandlerTests`, `DriverRaceFeatureTests`,
`TrainingRunsAndHoldoutTests`. `MlNetModelTrainer` itself has no test — a real AutoML search costs
30s per target — so exercise it end to end through `POST /api/models/train`.
Both test projects get `InternalsVisibleTo` from `F1Predictor.Application` and
`F1Predictor.Infrastructure`, so `internal` handlers, tools and prompt builders are directly
testable without a public surface just for tests. `tests/Directory.Build.props` turns off a
handful of analyzers that fire on legitimate test patterns (inline arrays, culture-sensitive
literals/comparisons, `using` disposal); `tests/.editorconfig` disables `CA1707` (test names
read as `Method_Scenario_Expectation`, not PascalCase) and `CA1812` (xunit test classes look
"uninstantiated" to the analyzer because the runner instantiates them via reflection). CI
(`.github/workflows/ci.yml`) runs `dotnet test F1Predictor.slnx` as part of `Build & Verify`.

## Known Scope Cuts

These are deliberate boundaries for a first release, not oversights:

- No tyre/strategy features — the `stints` endpoint is not ingested.
- Training can span seasons (`?fromYear=`), but only 2023 onwards exists on OpenF1's free
  tier — a few thousand driver-race rows at most. Not enough for the predictions to be taken
  as authoritative.
- Three of the five features (`PitStopCount`, `AvgPitStopDuration`, `Rainfall`) are only known
  after the race — see "Known leakage" in `ml-pipeline.md`.
- `QualiGapToPole = 99` sentinel rather than proper imputation for a missing qualifying lap.
- Ingestion runs synchronously inside the request. A background queue is a later concern.
- `LegacySqliteImporter` and its SQLite package reference exist only to migrate the
  original console prototype's data; they are Development-only and slated for removal.

## Current focus: the model

The AI layer is **paused** (`Ai:Enabled=false`). It is complete but switched off: hosted OpenAI
provider, classified-race explanations, background preview refresh, and pgvector race search.
The work now is making the classifiers honest and then better. The evaluation harness
(race-by-time split, grid baseline, `ModelTrainingRuns` log) is in place. The roadmap, judged
run by run against `BaselineAuc` in `GET /api/models/runs`:

1. Remove the post-race features (pit count, pit duration, rainfall) and record the honest AUC.
2. Ingest 2023 onwards and train with `fromYear=2023`; watch validation AUC and log loss.
3. Pre-race form features (rolling finish, team form, shrunk DNF rate, circuit history,
   teammate quali gap), computed only from earlier races and mirrored in the preview.
4. Replace the 99 sentinel with an imputed value plus a "no quali time" flag.
5. Model selection: longer AutoML budget, then hand-tune one trainer (LightGBM).
6. Calibration and walk-forward evaluation (train on races before k, score race k).

## Other next steps

- Add the `stints` endpoint → tyre compound / strategy features.
- Move ingestion onto a background queue so the endpoint returns `202` immediately.
- Retire the legacy SQLite import path.

Deliberately deferred: user accounts (ASP.NET Identity) and paid credits for the analyst chat.
Revisit only once the model is worth narrating and there is real demand; the running cost is
already bounded by `Ai:DailyChatRequestCap`.
