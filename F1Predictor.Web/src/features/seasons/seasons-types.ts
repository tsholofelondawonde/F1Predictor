// Serialized as an int by System.Text.Json's default enum handling (no JsonStringEnumConverter
// configured on the backend) — order must match F1Predictor.Application's IngestOutcome enum.
export enum IngestOutcome {
  Ingested = 0,
  AlreadyPresent = 1,
  NoRaceSession = 2,
  /** No longer produced by the backend — an unrun race is now reported as Scheduled. */
  NotClassified = 3,
  Scheduled = 4,
}

export const IngestOutcomeLabel: Record<IngestOutcome, string> = {
  [IngestOutcome.Ingested]: "Ingested",
  [IngestOutcome.AlreadyPresent]: "Already present",
  [IngestOutcome.NoRaceSession]: "No race session yet",
  [IngestOutcome.NotClassified]: "Not yet classified",
  [IngestOutcome.Scheduled]: "Scheduled",
};

export interface IngestedMeeting {
  meetingName: string;
  outcome: IngestOutcome;
  /** Points-scoring sessions stored with results. Two on a sprint weekend. */
  sessionsIngested: number;
  /** Sessions recorded without results because they have not run yet. */
  sessionsScheduled: number;
  resultCount: number;
  gridCount: number;
  pitStopCount: number;
  weatherReadingCount: number;
  driverEntryCount: number;
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
