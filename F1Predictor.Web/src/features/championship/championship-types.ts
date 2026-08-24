export interface DriverStanding {
  position: number;
  driverNumber: number;
  fullName: string;
  nameAcronym: string;
  teamName: string;
  teamColour: string;
  points: number;
  wins: number;
  podiums: number;
}

export interface ConstructorStanding {
  position: number;
  teamName: string;
  teamColour: string;
  points: number;
  wins: number;
  podiums: number;
  driverNumbers: number[];
}

export interface StandingsResponse {
  year: number;
  racesCompleted: number;
  sprintsCompleted: number;
  racesRemaining: number;
  sprintsRemaining: number;
  pointsStillAvailable: number;
  drivers: DriverStanding[];
  constructors: ConstructorStanding[];
}

export interface DriverForecast {
  position: number;
  driverNumber: number;
  fullName: string;
  nameAcronym: string;
  teamName: string;
  teamColour: string;
  points: number;
  wins: number;
  podiums: number;
  titleProbability: number;
  projectedPoints: number;
  projectedPointsLow: number;
  projectedPointsHigh: number;
  isMathematicallyAlive: boolean;
}

export interface ConstructorForecast {
  position: number;
  teamName: string;
  teamColour: string;
  points: number;
  wins: number;
  podiums: number;
  titleProbability: number;
  projectedPoints: number;
  projectedPointsLow: number;
  projectedPointsHigh: number;
  isMathematicallyAlive: boolean;
}

export interface ChampionshipForecastResponse {
  year: number;
  simulations: number;
  racesRemaining: number;
  sprintsRemaining: number;
  pointsStillAvailable: number;
  method: string;
  drivers: DriverForecast[];
  constructors: ConstructorForecast[];
}

export interface TitleScenario {
  position: number;
  driverNumber: number;
  fullName: string;
  nameAcronym: string;
  teamName: string;
  teamColour: string;
  points: number;
  pointsBehindLeader: number;
  maximumPossiblePoints: number;
  isLeader: boolean;
  isMathematicallyAlive: boolean;
  hasClinchedTitle: boolean;
  pointsToClinch: number | null;
  leaderMustAverageNoBetterThan: number | null;
  requiredPointsPerRace: number | null;
  requiredAveragePosition: number | null;
  titleProbability: number;
  requirement: string;
}

export interface TitleScenariosResponse {
  year: number;
  racesRemaining: number;
  sprintsRemaining: number;
  pointsStillAvailable: number;
  leaderName: string;
  contenders: TitleScenario[];
}
