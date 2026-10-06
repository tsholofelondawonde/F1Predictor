"use client";

import { useEffect } from "react";
import { create } from "zustand";
import { getAiStatus } from "@/features/analysis/analysis-service";
import type { AiStatusResponse } from "@/features/analysis/analysis-types";

/** What the UI assumes when the status call itself fails: no AI, so no buttons, no tab, no chat. */
const UNAVAILABLE: AiStatusResponse = {
  chatAvailable: false,
  embeddingsAvailable: false,
  provider: "None",
  model: null,
  embeddingModel: null,
};

interface AiStatusState {
  status: AiStatusResponse | null;
  /** True once a response (or a failure) has been recorded, so callers can tell "unknown" from "off". */
  loaded: boolean;
  load: () => Promise<void>;
}

// Module-level so every component asking on the same render shares one request rather than
// each firing its own — the tab nav, the preview card and the chat all ask on mount.
let inFlight: Promise<void> | null = null;

export const useAiStatusStore = create<AiStatusState>((set, get) => ({
  status: null,
  loaded: false,
  load: () => {
    if (get().loaded) return Promise.resolve();
    if (inFlight) return inFlight;

    inFlight = getAiStatus()
      .then((status) => set({ status, loaded: true }))
      .catch(() => set({ status: UNAVAILABLE, loaded: true }))
      .finally(() => {
        inFlight = null;
      });

    return inFlight;
  },
}));

/** Loads the AI status once per page load and reports whether the chat features are on. */
export function useAiStatus() {
  const status = useAiStatusStore((state) => state.status);
  const loaded = useAiStatusStore((state) => state.loaded);
  const load = useAiStatusStore((state) => state.load);

  useEffect(() => {
    void load();
  }, [load]);

  return { status, loaded, chatAvailable: status?.chatAvailable ?? false };
}
