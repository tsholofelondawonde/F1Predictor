# Project Status

## Testing

No automated test project exists yet. `F1Predictor.slnx` has an empty `/tests/` solution
folder left over from the CleanArchitectureGenerator scaffold, and no project in the
solution references xUnit, NUnit, MSTest, Moq, or NSubstitute. When tests are added,
record the chosen framework and conventions here rather than assuming any.

## Known Scope Cuts

- No tyre/strategy features (`stints` endpoint not ingested)
- Single season (~480 driver-race rows) — enough to prove the pipeline, not enough to trust
- `QualiGapToPole = 99` sentinel instead of proper imputation
- Ingestion runs synchronously inside the request; a background queue is a later concern
- `LegacySqliteImporter` and the SQLite package reference are temporary and should be
  deleted once the prototype's data is safely across

## Next Steps

1. Ingest 2–3 seasons instead of one for a less anemic training set
2. Add the `stints` endpoint → tyre compound/strategy features
3. Try the AutoML upgrade path once the SDCA baseline is trusted
4. React frontend: race selector + predicted probability table
5. Move ingestion onto a background queue so the endpoint returns 202 immediately
