import { api } from "@/shared/lib/api";
import type {
  HoldoutPredictionsResponse,
  NextRacePreviewResponse,
  RacePredictionsResponse,
} from "@/features/predictions/predictions-types";

export function getNextRacePreview(year: number): Promise<NextRacePreviewResponse> {
  return api.get<NextRacePreviewResponse>(`/seasons/${year}/next-race`).then((response) => response.data);
}

export function getRacePredictions(sessionKey: number): Promise<RacePredictionsResponse> {
  return api.get<RacePredictionsResponse>(`/races/${sessionKey}/predictions`).then((response) => response.data);
}

export function getHoldoutPredictions(year: number): Promise<HoldoutPredictionsResponse> {
  return api.get<HoldoutPredictionsResponse>(`/seasons/${year}/holdout`).then((response) => response.data);
}
