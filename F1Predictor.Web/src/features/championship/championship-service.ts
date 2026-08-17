import { api } from "@/shared/lib/api";
import type {
  ChampionshipForecastResponse,
  StandingsResponse,
  TitleScenariosResponse,
} from "@/features/championship/championship-types";

export function getStandings(year: number): Promise<StandingsResponse> {
  return api.get<StandingsResponse>(`/seasons/${year}/standings`).then((response) => response.data);
}

export function getChampionshipForecast(year: number): Promise<ChampionshipForecastResponse> {
  return api
    .get<ChampionshipForecastResponse>(`/seasons/${year}/championship-forecast`)
    .then((response) => response.data);
}

export function getTitleScenarios(year: number, topN = 5): Promise<TitleScenariosResponse> {
  return api
    .get<TitleScenariosResponse>(`/seasons/${year}/title-scenarios`, { params: { topN } })
    .then((response) => response.data);
}
