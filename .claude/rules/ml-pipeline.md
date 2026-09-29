# ML Pipeline

## Data Source: OpenF1

Base URL `https://api.openf1.org/v1/`, no auth, **2023 onwards only** on the free tier.

| Endpoint | Query by | Key fields |
|---|---|---|
| `meetings` | `year` | `meeting_key`, `year`, `circuit_short_name`, `country_name`, `meeting_name`, `date_start` |
| `sessions` | `meeting_key` | `session_key`, `meeting_key`, `session_name`, `session_type`, `date_start` |
| `starting_grid` | **qualifying** `session_key` | `position`, `driver_number`, `lap_duration` |
| `session_result` | **race** `session_key` | `driver_number`, `position` (nullable), `points`, `dnf`, `dns`, `dsq` |
| `pit` | **race** `session_key` | `driver_number`, `stop_duration` (null before the 2024 US GP — falls back to `lane_duration`), `lane_duration` |
| `weather` | **race** `session_key` | `rainfall` (0/1), `track_temperature` |
| `drivers` | any `session_key` | `driver_number`, `full_name`, `name_acronym`, `team_name`, `team_colour` |

**The grid comes from the qualifying session, not the race session.** This was verified
against live responses and contradicts what the original plan assumed, which is why
`RaceSession` carries a `QualifyingSessionKey` pointer. A sprint takes its order from the
**Sprint Qualifying** session instead — also verified live.

**Sprints arrive down the same pipe as a Grand Prix**: OpenF1 gives them
`session_type: "Race"` with `session_name: "Sprint"`. They award championship points
(8-7-6-5-4-3-2-1) so they must be ingested, but they must never reach the models — see
Feature Engineering below.

**`session_result.points` is authoritative.** Taking OpenF1's own award rather than deriving
points from finishing position means the sprint scale and any future rule change need no code
here. Only races that have not happened are scored from a table (`ChampionshipPoints`).

**`drivers` is populated for sessions that have not run yet**, which is what makes an entry
list — and therefore a preview — available before anyone has driven. It is also the only
source of driver names and teams anywhere in the system; everything else keys on car number.

Two caveats found in live data, both handled:

- OpenF1 has rounds it never published a result for (the 2026 Bahrain and Saudi Arabian
  Grands Prix among them). They stay unclassified for ever, so "still to run" means
  unclassified **and** in the future — otherwise they inflate the points still available.
- Free-tier coverage starts at 2023, and pre-season testing meetings have no race session.

Field names are snake_case throughout, so `OpenF1HttpClient` applies
`JsonNamingPolicy.SnakeCaseLower` rather than annotating every DTO property.

Politeness is enforced by `PolitenessDelayHandler`, a `DelegatingHandler` that spaces
requests ~500ms apart across all callers. Retries and 429 handling come from the standard
resilience pipeline, not hand-rolled loops.

## Feature Engineering

All rules live on `DriverRaceFeature` in the Domain, so there is exactly one definition of
each. Per race session with both grid and result data:

- `polePace` = min `lap_duration` across the grid (drivers with no time excluded)
- `rainedInSession` = any weather row with `rainfall > 0`
- Per driver in `session_result` where `!Dns` and `Position` is not null:
  - `GridPosition` — their grid position, or `grid count + 1` for a pit lane start
  - `QualiGapToPole` — `lap_duration - polePace`, or the `99` sentinel if they set no time
  - `PitStopCount`, `AvgPitStopDuration` — from `stop_duration ?? lane_duration`
  - `Podium` = `Position <= 3 && !Dsq`; `PointsFinish` = `Position <= 10 && !Dsq`

`RebuildFeaturesCommand` clears and regenerates the whole table rather than updating it
incrementally — cheap at this volume, and it rules out stale-feature bugs.

**Grands Prix only.** Both `RebuildFeaturesCommandHandler` and `SeasonFeatureSet.LoadAsync`
filter `!IsSprint && IsClassified`. This is load-bearing, not tidiness: a sprint has both a
grid and results, so without the filter it silently becomes a training row — and a third of
the distance, an 8-point scale and usually no pit stop make "podium" and "points finish" mean
something else entirely there. If a feature rebuild ever reports a suspiciously high race
count, this filter is the first thing to check.

## Model Training

Two independent binary classifiers over the same five features, differing only in label:

```
mlContext.Auto()
  .CreateBinaryClassificationExperiment(MaxExperimentTimeInSeconds: 30, OptimizingMetric: F1Score)
  .Execute(trainView, validationView, labelColumnName: "Label")
```

AutoML searches over SDCA, LBFGS, LightGBM, FastTree, and FastForest, building its own
featurization (concatenation, missing-value handling, calibration) internally — there is no
longer a hand-built `Concatenate`/`NormalizeMinMax` step. `ModelTrainingResult.TrainerName`
records which trainer the search picked for each target; the two classifiers may land on
different trainers since they are fit independently.

**Seasons.** `POST /api/models/train?year=&fromYear=` trains on every Grand Prix from `fromYear`
(default `year`) through `year`. The holdout is always the latest race **of `year` itself**
(`SeasonFeatureSet.Holdout`), so any window holds out the same race the holdout page scores.

**The split is by race and by time, never by row** (`Features/Predictions/Evaluation/RaceTimeSplit.cs`).
With the holdout set aside, the remaining races are ordered by date. The latest ~20% (at least
two) validate, and everything earlier trains. The split lives in Application because it is
policy; `IModelTrainer` receives a ready-made `TrainingData(Train, Validation)` and never
re-splits. It replaced a random 80/20 row split that leaked in two ways:
- a race's rows landed on both sides, so the model was "validated" on races it had partly seen;
- later races trained a model that was then scored on earlier ones.

A prediction is always about a race that hasn't happened, so validation has to look like that.
Five races (holdout + 2 train + 2 validation) is the minimum to train.

**Metrics come from the train-only fit; the shipped model is refitted.** `MlNetModelTrainer`
evaluates `BestRun.Model` on the validation races. It then refits `BestRun.Estimator` on train +
validation and saves *that*, so the served model uses every non-holdout race.

**Every run is recorded** in `ModelTrainingRuns` (append-only, one row per target) and listed
newest first by `GET /api/models/runs?target=&take=`. Each row holds:
- the season window, the trainer and the feature names
- validation AUC, F1, log loss, AUPRC, precision and recall
- `BaselineAuc`: grid order alone on the same validation rows
- holdout AUC and log loss, scored through the saved files via `IRacePredictor`
- `?notes=` from the train call

The response adds `AucOverBaseline`. Log loss is in **bits** throughout — ML.NET's unit, pinned by
`LogLossUnitTests` — so validation and holdout numbers compare directly. The model files are
still overwritten each run; the table is the experiment log, not a model registry.

The holdout page (`GetHoldoutPredictionsQueryHandler`) sets `ModelWarning` when the newest run
held out a different race, or when no run is recorded. The predictions are still served, but
they may be recitals of training data rather than a test.

Models are saved to the directory configured at `MachineLearning:ModelDirectory` (default
`models/`).

**Reproducibility is best-effort, not guaranteed**, unlike the rest of this codebase's stricter
determinism stance (see the Championship Forecasting section's `DeterministicRandom`). ML.NET's
AutoML only exposes a wall-clock time budget, not a trial-count knob, so re-running training is
not guaranteed to reach the same trainer or metrics bit-for-bit — this is an accepted trade-off
for search quality within a bounded time, not an oversight.

**Read AUC against the baseline, plus log loss and F1 — never accuracy.** Podium is ~15% of rows,
so "always predict no" scores ~85% accuracy while being useless. `TrainModelsResponse` carries
this warning in the payload itself for that reason. AUC near 0.5 is noise; a model that can't
beat `BaselineAuc` has learned nothing the grid didn't already say.

**Known leakage still in the feature set** (deliberately left for the feature-engineering work):
`PitStopCount`, `AvgPitStopDuration` and `Rainfall` are only known *after* the race. Pit count
also tracks the label, because a retiree stops less. At preview time they are replaced by season
averages and rain = 0, so the model is served inputs it never trained on. Expect validation AUC
to *drop* when they are removed; that drop is the honest number.

### How to add (or remove) a feature

Every touch point, in order:
1. `Domain/Predictions/DriverRaceFeature.cs`: the property, and its rule in `Create`. It must use
   only information available **before the race starts**. A rolling or form feature may only
   look at races strictly earlier than the row's own date.
2. `RebuildFeaturesCommandHandler`, if the rule needs raw data it doesn't load yet.
3. A migration (`dotnet ef migrations add …`, see `setup.md`). Rehearse it on a Neon branch and
   apply it to production by hand before merge.
4. `Infrastructure/MachineLearning/RaceFeatureInput.cs`: the property, `From`, `FeatureNames` and
   `ValuesByName`.
5. **Train/serve parity**: the next-race preview builds feature rows in memory
   (`PreviewNextRace/StartingGridProjection.cs`, `NextRaceContext`), and the new feature must be
   computed there the same way. A feature that exists only at training time is the leak above
   all over again.
6. Tests: `DriverRaceFeatureTests` for the rule, and the explainer's `SyntheticModels` if the
   feature count matters there.
7. Rebuild → train with `?notes=` describing the change → compare in `GET /api/models/runs`.

## Next-Race Preview

The classifiers need `GridPosition` and `QualiGapToPole`, which do not exist until qualifying
has run — which is most of the week. Rather than show nothing until Saturday,
`PreviewNextRaceQueryHandler` predicts either way and says which it did:

- **Grid published** → real `StartingGridEntry` rows, `gridConfirmed: true`.
- **Not yet** → a grid projected from recent form, `gridConfirmed: false`, and the UI labels it.

The projection (`StartingGridProjection.FromRecentForm`) is a *ranking* of each driver's
recency-weighted average gap to pole, not the averages themselves — the model was fitted on
grid slots 1..20 and would never have seen a field where six drivers start "4.3rd". Ranking on
gap rather than on average grid position keeps the two features rank-consistent, as they are in
a real session. Pit-stop inputs come from the driver's season average; rainfall is always 0,
because nothing here forecasts weather.

Feature rows are built in memory and handed straight to `IRacePredictor` — nothing requires
they be persisted. Sprints are never previewed, for the reason given under Feature Engineering
above.

## Championship Forecasting

Lives in `F1Predictor.Domain/Championship/`, entirely pure and dependency-free.

The SDCA classifiers cannot answer "who wins the title": they need a grid a future race does
not have, and two marginal probabilities do not describe a finishing order — and a championship
is decided by orders, not margins. So:

1. **`DriverFormModel`** fits a Plackett–Luce strength per driver from the season's Grand Prix
   finishing orders, by the standard MM iteration (Hunter, 2004). Three choices earn their
   place at ~14 races:
   - **Recency weighting**, half-life 5 races — a car developed over a season is not one car.
   - **Retirements censored**, not scored as last place. A blown engine says nothing about pace;
     reliability is modelled separately as a per-driver DNF rate shrunk toward the field mean.
   - **Top-10 partial ranking** rather than the full order. This one was found empirically: with
     the full order, a driver with six wins and one fifteenth place fitted *below* midfielders,
     because P(fifteenth) for a quick driver is so small that one bad race outweighs six wins.
     It also removes an asymmetry — censoring retirements protects a driver who crashes out
     while punishing one who limps home. Modelling only the points-paying positions fixes both.
2. **`ChampionshipSimulator`** plays the remaining calendar out 10,000 times, sampling each
   session's order via the Gumbel-max trick (one sort per session, not n sequential draws),
   dropping sampled retirements to the back with nothing, awarding real points, and settling
   both tables by count-back. Title probability is the share of seasons an entrant finished top.
3. **`TitleScenarioAnalyser`** answers the same question as arithmetic instead: who is
   mathematically alive, what the leader needs to clinch, and — assuming a contender wins out —
   the best average finish the leader could still manage and lose.

**The seed is fixed at 42, and that matters.** The dashboard re-reads the forecast on a timer;
an unseeded run would jitter the headline number by tenths on every poll while saying nothing.
`CachedForecast` keys on the latest classified session, so a new result invalidates it by
itself and the five-minute expiry is only a backstop.

`DeterministicRandom` is a hand-rolled xoshiro256++ rather than `System.Random` on purpose:
the framework makes no promise a given seed yields the same sequence across .NET releases, so
a shared seed alone would not keep published odds stable.

**Honest limits**: one season is a thin basis for a pace estimate, and nothing here knows about
upgrades, penalties, weather, or a driver's record at a given circuit. The
`ChampionshipForecastResponse.Method` field carries this caveat in the payload, in the same
spirit as `TrainModelsResponse.MetricGuidance`.

## Explaining a prediction

AutoML may pick any of five trainers per target (SDCA, LBFGS, LightGBM, FastTree, FastForest —
see Model Training above), and the two committed models did not land on the same one: podium is a
linear model (calibrated `LinearBinaryModelParameters`), points-finish is `FastTree`. Reading
coefficients only works for the linear case, so `F1Predictor.Infrastructure/MachineLearning/
ModelExplainer.cs` uses ML.NET's `CalculateFeatureContribution` instead — it is the one
explanation method every trainer in the search space actually supports.

- Contributions are computed with `normalize: false`, so they stay on the score scale rather than
  a [0,1]-normalised one — that is what makes them comparable between drivers and, for a linear
  model, means `Σ contributions + bias == score` (cross-checked in
  `tests/F1Predictor.Infrastructure.Tests`). For a tree model the contributions are per-path, not
  summands of a linear equation, but they are still signed and comparable the same way.
- Feature slot names are read off the saved model's feature column schema — the column
  `predictor.FeatureColumnName` names, which AutoML calls `__Features__`, not a literal
  `Features` — via `DataViewSchema.Column.GetSlotNames`, not hand-maintained, so they can never
  drift from what the model was actually trained on; `RaceFeatureInput.FeatureNames` is only the
  fallback when a model has no slot names at all.
- `Direction` ("helps"/"hurts"/"neutral") is derived in the handler
  (`TargetExplanationResponse.DirectionOf`) from the sign of the contribution — the model itself
  has no notion of direction, only a signed number.
- Finding the underlying trainer to hand to `CalculateFeatureContribution` needs an adapter: every
  trainer's `Fit()` returns a `BinaryPredictionTransformer<TModel>` closed over its own model type,
  and CLR variance can't cast that to
  `ISingleFeaturePredictionTransformer<ICalculateFeatureContribution>` even though the runtime
  instance supports it. `ModelExplainer.FindPredictionTransformer` walks the transformer chain from
  the end and wraps the match in `FeatureContributionTransformerAdapter` to bridge the gap — this
  is a real ML.NET limitation, not a shortcut.
- A narrative sentence on top of the contribution table is optional: it only appears when an AI
  provider is configured (see "AI analyst" below), and is cached for 10 minutes via
  `CachedNarrative` (`Microsoft.Extensions.Caching.Hybrid`), keyed on
  `explain:{sessionKey}:{driverNumber}:{gridConfirmed}:{model}` so the same driver, the same
  race, the same grid state (projected vs. confirmed) and the same chat model don't re-prompt
  the LLM on every page view — but a grid that flips from projected to confirmed does.

## AI analyst

> **Paused.** `appsettings.json` sets `Ai:Enabled=false` while work focuses on the model. The
> code and the pgvector schema are intact; set `Ai:Enabled=true` (plus a provider) to resume.

An `IChatClient` (`Microsoft.Extensions.AI`) sits beside the classifiers and the simulator. It is
backed by OllamaSharp against a local Ollama daemon (`Ai:Provider=Ollama`) or by the OpenAI SDK
(`Ai:Provider=OpenAi`, which also accepts any OpenAI-compatible endpoint). It narrates and
answers questions; it never predicts — every number it states must come from a feature
contribution, a query handler result, or a tool call, never from the model's own "reasoning".

`Ai:Provider` defaults to `None`, which fails closed exactly like `Security:ApiKey`:
- An `UnavailableChatClient` is registered, so handler constructors always resolve.
- Handlers guard on `IAiCapabilities.ChatAvailable` before ever calling the client.

Embeddings (`Ai:Embeddings:Provider`, pgvector-backed race search) are gated the same way on
`EmbeddingsAvailable`.

**`Ai:Enabled` is the master switch.** `false` is folded into both provider settings as `None`,
by `AiOptions.ApplyMasterSwitch` (options post-configure) and `AiOptions.Read` (the eager reads
in `AddAi`/`AddScheduler`). Every downstream `Provider` check therefore keeps working unchanged,
and keys in user secrets need not be deleted to turn AI off. With AI off:
- the ingestion coordinator does not trigger `AnalysisRefreshJob`;
- `/api/races/{sessionKey}/similar` returns `Analysis.AiUnavailable`;
- the frontend hides AI surfaces through `WhenAiAvailable`.

`AiOptions` defaults: `Ollama.Endpoint` `http://localhost:11434`, `Ollama.Model` `llama3.1:8b` (any
Ollama model with tool support), `Ollama.ContextLength` **16384** — Ollama's own default of 4096 is
too small once a system prompt and a couple of tool results are in context, and the failure mode
is a silently truncated conversation, not an error. `TimeoutSeconds` 120 with **no retries**: a
generation that already took a minute and failed is worse to retry than to fail. `MaxToolIterations`
6 caps tool-call round trips per request. `Temperature` 0.2 — narration should be repeatable, not
creative.

**Tools** (`F1Predictor.Application/Features/Analysis/AnalystTools.cs`) are thin `AIFunction`s over
the *existing* query handlers, so the analyst can only ever surface numbers the rest of the API
already serves:

| Tool | Returns |
|---|---|
| `get_next_race_preview` | Podium/points probabilities for every driver in the next Grand Prix, and whether the grid is real or projected |
| `explain_driver(driverNumber)` | The per-feature contribution breakdown behind one driver's next-race prediction |
| `get_standings` | Top-10 drivers' and all constructors' championship tables |
| `get_championship_forecast` | Top-8 title odds from the Monte Carlo simulation, with the method caveat |
| `get_title_scenarios(topN=5)` | What each leading contender needs to win the drivers' title |
| `get_season_races` | The season calendar with session keys, sprint/classified flags |
| `get_race_predictions(sessionKey)` | Per-driver predicted probability vs. actual result for a classified race |

Each tool returns a small DTO rounded to 2 dp and keyed by driver acronym rather than full name, so
an 8B model's context survives a question that needs two or three tool calls. A failed lookup comes
back as `{ unavailable: true, reason }`, never an exception — the model says "the model doesn't
track that" instead of the request dying. The season is bound from the command, not chosen by the
model, so it cannot wander into another year.

**Prompt rules** (`AnalystPrompts.cs`) are composable blocks — Identity, Grounding, NoSpeculation,
Brevity — mixed with one task-specific block per use case (driver explanation, race preview,
analyst chat), so the "say 'the model', not 'I predict'" and "don't speculate about weather,
upgrades, penalties" rules are defined exactly once and shared by all three.

**SSE event vocabulary** (`POST /api/ai/ask`, `AnalystStream.Map`): `status` while a tool call is in
flight (a human-readable line from `AnalystTools.StatusFor`, e.g. "Running the title odds…"),
`delta` for each text fragment, `done` at the end of a normal stream, `error` on a transport fault
(logged server-side with the real exception; the client only ever sees a generic message).

**Preview persistence**: `GenerateRacePreviewCommand` writes (upserts) a `RacePreviewNarrative` row
per session key; `GetRacePreviewQuery` reads it back. This is what lets production — where
`Ai:Provider` is `None` — still serve a preview: whichever environment generated it (typically a
developer running Ollama locally) writes the row once, and prod's `GET` is read-only from then on;
only the `POST` route requires `ChatAvailable` and returns `Analysis.AiUnavailable` otherwise.

**The `Stale` rule** (`GetRacePreviewQueryHandler`): a preview is stale when the real starting grid
has since been published for a preview generated from a projected one, **or** a newer race in the
previewed race's own season has been classified since generation
(`narrative.BasedOnLatestClassifiedSessionKey` vs. the season's current latest classified session
key). A `null` basis — recorded when the preview was generated before any race in that season had
been classified — becomes stale the moment any classified session exists, not just a later one.
The comparison is deliberately scoped to the previewed race's season: `BasedOnLatestClassifiedSessionKey`
was set from `SeasonChampionship.LatestClassifiedSessionKey`, which only looks at that season, so
comparing against a different season's latest classified race would flag every preview stale.
