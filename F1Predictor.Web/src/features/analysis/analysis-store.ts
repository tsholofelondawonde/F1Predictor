import { create } from "zustand";
import { askAnalyst } from "@/features/analysis/analysis-service";
import type { ChatTurn } from "@/features/analysis/analysis-types";
import { ApiError } from "@/shared/lib/api-error";
import { getErrorDisplay } from "@/shared/lib/error-display";

export interface ChatMessageState {
  id: string;
  role: "user" | "assistant";
  content: string;
  /** The "calling tool" line shown under an assistant bubble while a tool runs. */
  status?: string | null;
  streaming: boolean;
  error?: string | null;
}

interface AnalysisState {
  messages: ChatMessageState[];
  streaming: boolean;
  send: (year: number, text: string) => Promise<void>;
  abort: () => void;
  clear: () => void;
}

/** The backend caps a conversation at 20 turns, so the prior 19 plus the new question is the most it will take. */
const MAX_TURNS = 20;

let controller: AbortController | null = null;
let nextId = 0;

function newId(): string {
  nextId += 1;
  return `msg-${nextId}`;
}

function isAbortError(error: unknown): boolean {
  // `fetch` rejects with a DOMException, which extends Error in every browser that has ReadableStream.
  return error instanceof Error && error.name === "AbortError";
}

function describeError(error: unknown): string {
  if (error instanceof ApiError) return getErrorDisplay(error).message;
  return "Something went wrong unexpectedly.";
}

export const useAnalysisStore = create<AnalysisState>((set, get) => ({
  messages: [],
  streaming: false,

  send: async (year, text) => {
    const content = text.trim();
    if (!content || get().streaming) return;

    // The validator rejects an empty turn, so a reply that was stopped before any text arrived
    // (or failed outright) stays visible in the list but is not replayed to the analyst.
    const priorTurns: ChatTurn[] = get()
      .messages.filter((message) => message.content.length > 0)
      .map(({ role, content: turnContent }) => ({ role, content: turnContent }));
    const turns = [...priorTurns, { role: "user" as const, content }].slice(-MAX_TURNS);

    const assistantId = newId();
    controller = new AbortController();

    set((state) => ({
      streaming: true,
      messages: [
        ...state.messages,
        { id: newId(), role: "user", content, streaming: false },
        { id: assistantId, role: "assistant", content: "", status: null, streaming: true, error: null },
      ],
    }));

    const patchAssistant = (update: (message: ChatMessageState) => ChatMessageState) =>
      set((state) => ({
        messages: state.messages.map((message) => (message.id === assistantId ? update(message) : message)),
      }));

    try {
      await askAnalyst(
        year,
        turns,
        (event) => {
          switch (event.type) {
            case "status":
              patchAssistant((message) => ({ ...message, status: event.text }));
              break;
            case "delta":
              patchAssistant((message) => ({
                ...message,
                content: message.content + (event.text ?? ""),
                status: null,
              }));
              break;
            case "done":
              patchAssistant((message) => ({ ...message, status: null, streaming: false }));
              break;
            case "error":
              patchAssistant((message) => ({
                ...message,
                status: null,
                error: event.text ?? "The analyst could not answer.",
              }));
              break;
          }
        },
        controller.signal,
      );
    } catch (error) {
      // Stop is the reader's own doing: keep whatever streamed so far and say nothing.
      if (!isAbortError(error)) {
        patchAssistant((message) => ({ ...message, status: null, error: describeError(error) }));
      }
    } finally {
      controller = null;
      patchAssistant((message) => ({ ...message, status: null, streaming: false }));
      set({ streaming: false });
    }
  },

  abort: () => {
    controller?.abort();
  },

  clear: () => {
    controller?.abort();
    set({ messages: [] });
  },
}));
