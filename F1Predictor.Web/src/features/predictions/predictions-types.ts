export interface DriverPrediction {
  driverNumber: number;
  gridPosition: number;
  finishPosition: number;
  actualPodium: boolean;
  actualPointsFinish: boolean;
  podiumProbability: number;
  pointsProbability: number;
}

export interface RacePredictionsResponse {
  sessionKey: number;
  meetingName: string;
  drivers: DriverPrediction[];
}

export interface HoldoutPredictionsResponse {
  year: number;
  sessionKey: number;
  meetingName: string;
  circuitShortName: string;
  dateStart: string;
  drivers: DriverPrediction[];
}
