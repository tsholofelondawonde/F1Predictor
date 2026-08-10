// Serialized as an int by System.Text.Json's default enum handling (no JsonStringEnumConverter
// configured on the backend) — order must match F1Predictor.Application's IngestOutcome enum.
export enum IngestOutcome {
  Ingested = 0,
  AlreadyPresent = 1,
  NoRaceSession = 2,
  NotClassified = 3,
}

export const IngestOutcomeLabel: Record<IngestOutcome, string> = {
  [IngestOutcome.Ingested]: "Ingested",
  [IngestOutcome.AlreadyPresent]: "Already present",
  [IngestOutcome.NoRaceSession]: "No race session yet",
  [IngestOutcome.NotClassified]: "Not yet classified",
};

export interface IngestedMeeting {
  meetingName: string;
  outcome: IngestOutcome;
  resultCount: number;
  gridCount: number;
  pitStopCount: number;
  weatherReadingCount: number;
}

export interface IngestSeasonResponse {
  year: number;
  meetingsFound: number;
  meetingsIngested: number;
  meetings: IngestedMeeting[];
}

export interface SeasonRace {
  sessionKey: number;
  meetingName: string;
  circuitShortName: string;
  countryName: string;
  dateStart: string;
  featureRowCount: number;
}

export interface RebuildFeaturesResponse {
  racesWithFeatures: number;
  racesSkipped: number;
  featureRows: number;
}
