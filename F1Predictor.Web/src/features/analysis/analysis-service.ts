import type {
  AiStatusResponse,
  AnalystEvent,
  AnalystEventType,
  ChatTurn,
  DriverExplanationResponse,
  RacePreviewResponse,
} from "@/features/analysis/analysis-types";
import { api } from "@/shared/lib/api";
import { ApiError } from "@/shared/lib/api-error";
import { readSseStream } from "@/shared/lib/sse";
import type { ProblemDetails } from "@/shared/types/problem-details";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5286";

export const getAiStatus = () => api.get<AiStatusResponse>("/ai/status").then((r) => r.data);
export const getRacePreview = (sessionKey: number) =>
  api.get<RacePreviewResponse>(`/races/${sessionKey}/preview`).then((r) => r.data);
export const generateRacePreview = (sessionKey: number) =>
  api.post<RacePreviewResponse>(`/races/${sessionKey}/preview`).then((r) => r.data);
export const explainDriver = (year: number, driverNumber: number) =>
  api.get<DriverExplanationResponse>(`/seasons/${year}/next-race/drivers/${driverNumber}/explanation`).then((r) => r.data);

const EVENT_TYPES: readonly AnalystEventType[] = ["status", "delta", "done", "error"];

function isAnalystEventType(event: string): event is AnalystEventType {
  return (EVENT_TYPES as readonly string[]).includes(event);
}

/** EventSource cannot POST, so this is fetch + a hand-rolled SSE reader. */
export async function askAnalyst(
  year: number,
  messages: ChatTurn[],
  onEvent: (event: AnalystEvent) => void,
  signal: AbortSignal,
): Promise<void> {
  const response = await fetch(`${API_URL}/api/ai/ask`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "text/event-stream",
      "X-Api-Key": process.env.NEXT_PUBLIC_API_KEY ?? "",
    },
    body: JSON.stringify({ year, messages }),
    signal,
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null;
    throw ApiError.fromProblemDetails(response.status, problem);
  }
  if (!response.body) throw new ApiError(0, "Network", "The server sent no stream.");

  await readSseStream(response.body, (event, data) => {
    const parsed = JSON.parse(data) as AnalystEvent;
    // The server names every event after its type; an unnamed ("message") event falls back to the payload's.
    onEvent({ ...parsed, type: isAnalystEventType(event) ? event : parsed.type });
  });
}
