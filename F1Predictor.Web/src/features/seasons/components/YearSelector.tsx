"use client";

import { useSeasonsStore } from "@/features/seasons/seasons-store";

const OPENF1_FIRST_YEAR = 2023;

export function YearSelector() {
  const selectedYear = useSeasonsStore((state) => state.selectedYear);
  const setSelectedYear = useSeasonsStore((state) => state.setSelectedYear);
  const currentYear = new Date().getFullYear();

  return (
    <label className="flex items-center gap-2 text-sm">
      <span className="font-medium text-(--color-foreground)">Season</span>
      <select
        value={selectedYear}
        onChange={(event) => setSelectedYear(Number(event.target.value))}
        className="rounded-md border border-(--color-border) bg-(--color-surface) px-3 py-1.5"
      >
        {Array.from({ length: currentYear - OPENF1_FIRST_YEAR + 1 }, (_, i) => currentYear - i).map((year) => (
          <option key={year} value={year}>
            {year}
          </option>
        ))}
      </select>
    </label>
  );
}
