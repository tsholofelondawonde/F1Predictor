import { create } from "zustand";
import type { IngestSeasonResponse } from "@/features/seasons/seasons-types";

export type IngestStatus = "idle" | "running" | "success" | "error";

interface SeasonsState {
  selectedYear: number;
  ingestStatus: IngestStatus;
  lastIngestResponse: IngestSeasonResponse | null;
  lastIngestError: string | null;
  setSelectedYear: (year: number) => void;
  setIngestRunning: () => void;
  setIngestSuccess: (response: IngestSeasonResponse) => void;
  setIngestError: (message: string) => void;
}

const currentYear = new Date().getFullYear();

export const useSeasonsStore = create<SeasonsState>((set) => ({
  selectedYear: currentYear,
  ingestStatus: "idle",
  lastIngestResponse: null,
  lastIngestError: null,
  setSelectedYear: (year) => set({ selectedYear: year }),
  setIngestRunning: () => set({ ingestStatus: "running", lastIngestError: null }),
  setIngestSuccess: (response) => set({ ingestStatus: "success", lastIngestResponse: response }),
  setIngestError: (message) => set({ ingestStatus: "error", lastIngestError: message }),
}));
