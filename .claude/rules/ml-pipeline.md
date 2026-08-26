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
Concatenate("Features", GridPosition, QualiGapToPole, PitStopCount, AvgPitStopDuration, Rainfall)
  -> NormalizeMinMax("Features")
  -> SdcaLogisticRegression(labelColumnName: "Label")
```

80/20 split, seed 42 for reproducibility. Models are saved to the directory configured at
`MachineLearning:ModelDirectory` (default `models/`).

**Read AUC and F1, never accuracy.** Podium is ~15% of rows, so "always predict no" scores
~85% accuracy while being useless. `TrainModelsResponse` carries this warning in the payload
itself for that reason. AUC near 0.5 is noise; grid position alone should clear 0.65.

Not AutoML: ML.NET's AutoML API is still preview, and a working baseline should not depend
on a shifting surface. Swapping it in later means replacing pipeline construction only.

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
