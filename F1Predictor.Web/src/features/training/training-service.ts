import { api } from "@/shared/lib/api";
import type { TrainModelsResponse } from "@/features/training/training-types";

export function trainModels(year: number): Promise<TrainModelsResponse> {
  return api.post<TrainModelsResponse>("/models/train", null, { params: { year } }).then((response) => response.data);
}
