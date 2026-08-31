# Project Status

## Testing

There is no automated test project yet. `F1Predictor.slnx` has an empty `/tests/` solution
folder from the CleanArchitectureGenerator scaffold, and no project references a test
framework. When tests are added, record the chosen framework and conventions here rather
than assuming any. Contributions here are especially welcome — see `CONTRIBUTING.md`.

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
3. Try the AutoML upgrade path once the SDCA baseline is trusted.
4. Move ingestion onto a background queue so the endpoint returns `202` immediately.
5. Retire the legacy SQLite import path.
