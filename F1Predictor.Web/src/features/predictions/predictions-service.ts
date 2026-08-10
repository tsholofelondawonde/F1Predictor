import { api } from "@/shared/lib/api";
import type { HoldoutPredictionsResponse, RacePredictionsResponse } from "@/features/predictions/predictions-types";

export function getRacePredictions(sessionKey: number): Promise<RacePredictionsResponse> {
  return api.get<RacePredictionsResponse>(`/races/${sessionKey}/predictions`).then((response) => response.data);
}

export function getHoldoutPredictions(year: number): Promise<HoldoutPredictionsResponse> {
  return api.get<HoldoutPredictionsResponse>(`/seasons/${year}/holdout`).then((response) => response.data);
}
