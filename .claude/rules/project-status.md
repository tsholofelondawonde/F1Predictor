# Project Status

## Testing

Two test projects live under `/tests/`: `F1Predictor.Application.Tests` (42 tests) and
`F1Predictor.Infrastructure.Tests` (8 tests), both xunit 2.9.3 + FluentAssertions 8.9.0. Test
methods are named `Method_Scenario_Expectation`. Fakes are hand-written under each project's
`Fakes/` folder (`FakeChatClient`, `FakeRacePredictor`, `FakeAiCapabilities`, `InMemoryDb`, and
so on) in preference to Moq — Moq is pinned in `Directory.Packages.props` for the rare case a
hand-written fake isn't worth it, but the AI-layer tests use none. Both test projects get
`InternalsVisibleTo` from `F1Predictor.Application` and `F1Predictor.Infrastructure`, so
`internal` handlers, tools and prompt builders are directly testable without a public surface
just for tests. `tests/Directory.Build.props` and `tests/.editorconfig` hold the test-only
analyzer relaxations (naming, mocking-friendly patterns) that would otherwise fail the repo's
`TreatWarningsAsErrors` build outside test code. CI (`.github/workflows/ci.yml`) runs
`dotnet test F1Predictor.slnx` as part of `Build & Verify`.

## Known Scope Cuts

These are deliberate boundaries for a first release, not oversights:

- No tyre/strategy features — the `stints` endpoint is not ingested.
- Single season of training data (~480 driver-race rows). Enough to demonstrate the
  pipeline end to end; not enough for the predictions to be taken as authoritative.
- `QualiGapToPole = 99` sentinel rather than proper imputation for a missing qualifying lap.
- Ingestion runs synchronously inside the request. A background queue is a later concern.
- `LegacySqliteImporter` and its SQLite package reference exist only to migrate the
  original console prototype's data; they are Development-only and slated for removal.

## Next Steps

1. Ingest 2–3 seasons instead of one for a less anemic training set.
2. Add the `stints` endpoint → tyre compound / strategy features.
3. Move ingestion onto a background queue so the endpoint returns `202` immediately.
4. Retire the legacy SQLite import path.

Explicitly deferred from the AI analysis layer — designed in a local, uncommitted stage-4
design doc (`docs/` is gitignored) rather than checked in here:

5. A hosted chat provider for production (OpenAI direct) so the analyst and generated previews
   work on the deployed site without a developer's local Ollama — config-only swap via
   `Ai:Provider`.
6. Explanations for already-classified races (`/api/races/{sessionKey}/predictions`), reusing
   the same `Explain` port and a different loader from the next-race explanation.
7. Background regeneration of the race preview after each ingest, so it stops being a button a
   human has to press.
8. Embeddings + semantic search over race fact sheets ("which race was most like this one?"),
   needing `pgvector` on Neon and an embedding model.
