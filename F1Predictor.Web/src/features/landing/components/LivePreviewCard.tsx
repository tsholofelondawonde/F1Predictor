"use client";

import { useCallback, useEffect, useState } from "react";
import { getNextRacePreview } from "@/features/predictions/predictions-service";
import { useLiveData } from "@/shared/lib/use-live-data";
import { ProbabilityBar } from "@/shared/components/ProbabilityBar";
import { TeamColour, teamColourCss } from "@/shared/components/TeamColour";

interface PreviewRow {
  driverNumber: number;
  fullName: string;
  teamName: string;
  teamColour: string;
  gridPosition: number;
  podiumProbability: number;
}

/**
 * Illustrative only — shown when the real preview errors (e.g. a fresh season with
 * no trained models yet) or takes too long, so the marketing hero never looks broken.
 */
const SAMPLE_DRIVERS: PreviewRow[] = [
  { driverNumber: 1, fullName: "Max Verstappen", teamName: "Red Bull Racing", teamColour: "3671C6", gridPosition: 1, podiumProbability: 0.71 },
  { driverNumber: 4, fullName: "Lando Norris", teamName: "McLaren", teamColour: "FF8000", gridPosition: 2, podiumProbability: 0.58 },
  { driverNumber: 16, fullName: "Charles Leclerc", teamName: "Ferrari", teamColour: "E8002D", gridPosition: 3, podiumProbability: 0.44 },
  { driverNumber: 63, fullName: "George Russell", teamName: "Mercedes", teamColour: "27F4D2", gridPosition: 4, podiumProbability: 0.31 },
];

const SAMPLE_FALLBACK_DELAY_MS = 4000;

export function LivePreviewCard() {
  const year = new Date().getFullYear();
  const fetcher = useCallback(() => getNextRacePreview(year), [year]);
  const { data, error, loading } = useLiveData(fetcher, [year]);
  const [timedOut, setTimedOut] = useState(false);

  useEffect(() => {
    if (!loading) return;

    const timer = setTimeout(() => setTimedOut(true), SAMPLE_FALLBACK_DELAY_MS);
    return () => clearTimeout(timer);
  }, [loading]);

  const useSample = Boolean(error) || (loading && timedOut) || (!loading && !data);

  const rows: PreviewRow[] = useSample ? SAMPLE_DRIVERS : (data?.drivers.slice(0, 4) ?? []);
  const meetingLabel = useSample ? "Next Grand Prix" : (data?.meetingName ?? "Loading…");
  const gridConfirmed = !useSample && Boolean(data?.gridConfirmed);

  const badgeLabel = useSample ? "Sample preview" : gridConfirmed ? "Confirmed grid" : "Projected grid";
  const badgeClass = useSample
    ? "bg-(--color-surface-hover) text-(--color-muted)"
    : gridConfirmed
      ? "bg-(--color-points)/15 text-(--color-points)"
      : "bg-(--color-podium)/15 text-(--color-podium)";

  return (
    <div className="rounded-xl border border-(--color-landing-border) bg-(--color-landing-surface) p-5 shadow-2xl shadow-(--color-accent-glow)">
      <div className="mb-4 flex items-center justify-between gap-3">
        <p className="truncate text-xs font-medium uppercase tracking-wide text-(--color-muted)">{meetingLabel}</p>
        <span className={`shrink-0 rounded-full px-2.5 py-1 text-[10px] font-medium ${badgeClass}`}>
          {badgeLabel}
        </span>
      </div>

      <ul className="space-y-3">
        {rows.map((driver) => (
          <li key={driver.driverNumber} className="flex items-center gap-3">
            <span className="w-4 font-mono text-xs tabular-nums text-(--color-muted)">{driver.gridPosition}</span>
            <TeamColour colour={driver.teamColour} title={driver.teamName} />
            <span className="w-32 shrink-0 truncate text-sm font-medium">{driver.fullName}</span>
            <div className="flex-1">
              <ProbabilityBar value={driver.podiumProbability} colour={teamColourCss(driver.teamColour)} />
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
