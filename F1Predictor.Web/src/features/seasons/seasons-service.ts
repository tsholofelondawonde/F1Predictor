import { api } from "@/shared/lib/api";
import type { IngestSeasonResponse, RebuildFeaturesResponse, SeasonRace } from "@/features/seasons/seasons-types";

// A full season ingest is verified to take 300s+ (a ~500ms politeness delay per OpenF1 call,
// across ~20+ meetings x ~5 calls each) — override the default axios timeout so the browser
// doesn't give up on a request the server is still legitimately working on.
const INGEST_TIMEOUT_MS = 600_000;

export function ingestSeason(year: number): Promise<IngestSeasonResponse> {
  return api
    .post<IngestSeasonResponse>(`/seasons/${year}/ingest`, null, { timeout: INGEST_TIMEOUT_MS })
    .then((response) => response.data);
}

export function getSeasonRaces(year: number): Promise<SeasonRace[]> {
  return api.get<SeasonRace[]>(`/seasons/${year}/races`).then((response) => response.data);
}

export function rebuildFeatures(): Promise<RebuildFeaturesResponse> {
  return api.post<RebuildFeaturesResponse>("/features/rebuild").then((response) => response.data);
}
