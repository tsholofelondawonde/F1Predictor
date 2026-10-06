// Serialized as an int — order must match F1Predictor.Application's PredictionTarget enum.
export enum PredictionTarget {
  Podium = 0,
  PointsFinish = 1,
}

/** Validation metrics — measured on the latest races before the holdout, which the model did not train on. */
export interface ModelTrainingResult {
  target: PredictionTarget;
  areaUnderRocCurve: number;
  f1Score: number;
  trainingRowCount: number;
  modelPath: string;
  trainerName: string;
  validationRowCount: number;
  /** In bits: 1.0 is a coin flip, lower is better. */
  logLoss: number;
  areaUnderPrecisionRecallCurve: number;
  positivePrecision: number;
  positiveRecall: number;
  featureNames: string[];
}

export interface TargetEvaluation {
  runId: number;
  /** AUC of ranking by grid position alone — the bar the model has to clear. */
  baselineAuc: number | null;
  holdoutAuc: number | null;
  holdoutLogLoss: number | null;
}

export interface TrainModelsResponse {
  year: number;
  racesTrainedOn: number;
  trainingRows: number;
  holdoutRaceName: string;
  podium: ModelTrainingResult;
  pointsFinish: ModelTrainingResult;
  metricGuidance: string;
  fromYear: number;
  validationRaceNames: string[];
  podiumEvaluation: TargetEvaluation;
  pointsFinishEvaluation: TargetEvaluation;
}
