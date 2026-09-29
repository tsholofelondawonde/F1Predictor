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
  sprintSessionKey: number | null;
  sprintDateStart: string | null;
  /** False until qualifying has run, in which case every grid position is projected from form. */
  gridConfirmed: boolean;
  drivers: PreviewDriver[];
  /** True only when the grid isn't confirmed but OpenF1 already has one — qualifying has run and just needs re-ingesting. */
  qualifyingReadyToIngest: boolean;
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
  /** Set when the models on disk can't be confirmed to have held this race out of training. */
  modelWarning: string | null;
}
