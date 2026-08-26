import { create } from "zustand";

interface SeasonsState {
  selectedYear: number;
  setSelectedYear: (year: number) => void;
}

const currentYear = new Date().getFullYear();

export const useSeasonsStore = create<SeasonsState>((set) => ({
  selectedYear: currentYear,
  setSelectedYear: (year) => set({ selectedYear: year }),
}));
