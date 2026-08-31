// Serialized as an int — order must match F1Predictor.Application's PredictionTarget enum.
export enum PredictionTarget {
  Podium = 0,
  PointsFinish = 1,
}

export interface ModelTrainingResult {
  target: PredictionTarget;
  areaUnderRocCurve: number;
  f1Score: number;
  trainingRowCount: number;
  modelPath: string;
}

export interface TrainModelsResponse {
  year: number;
  racesTrainedOn: number;
  trainingRows: number;
  holdoutRaceName: string;
  podium: ModelTrainingResult;
  pointsFinish: ModelTrainingResult;
  metricGuidance: string;
}
