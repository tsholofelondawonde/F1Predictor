export interface DriverPrediction {
  driverNumber: number;
  fullName: string;
  nameAcronym: string;
  teamName: string;
  teamColour: string;
  gridPosition: number;
  finishPosition: number;
  actualPodium: boolean;
  actualPointsFinish: boolean;
  podiumProbability: number;
  pointsProbability: number;
}

export interface PreviewDriver {
  driverNumber: number;
  fullName: string;
  nameAcronym: string;
  teamName: string;
  teamColour: string;
  gridPosition: number;
  qualiGapToPole: number;
  hasForm: boolean;
  podiumProbability: number;
  pointsProbability: number;
}

export interface NextRacePreviewResponse {
  year: number;
  sessionKey: number;
  meetingKey: number;
  meetingName: string;
  circuitShortName: string;
  countryName: string;
  dateStart: string;
  hasSprint: boolean;
  /** False until qualifying has run, in which case every grid position is projected from form. */
  gridConfirmed: boolean;
  drivers: PreviewDriver[];
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
