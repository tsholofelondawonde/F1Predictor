"use client";

import Link from "next/link";
import { useEffect, useState, type ReactNode } from "react";
import { useSeasonsStore } from "@/features/seasons/seasons-store";
import { getSeasonRaces } from "@/features/seasons/seasons-service";
import type { SeasonRace } from "@/features/seasons/seasons-types";
import { Card } from "@/shared/components/Card";
import { ApiError } from "@/shared/lib/api-error";
import { getErrorDisplay } from "@/shared/lib/error-display";

export function RaceList() {
  const selectedYear = useSeasonsStore((state) => state.selectedYear);
  const [races, setRaces] = useState<SeasonRace[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset before refetching on year change
    setRaces(null);
    setError(null);

    getSeasonRaces(selectedYear)
      .then((response) => {
        if (!cancelled) setRaces(response);
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof ApiError ? getErrorDisplay(err).message : "Could not load races.");
      });

    return () => {
      cancelled = true;
    };
  }, [selectedYear]);

  return (
    <Card title={`Races — ${selectedYear}`}>
      {error && <p className="text-sm text-(--color-error-text)">{error}</p>}
      {!error && races === null && <p className="text-sm text-(--color-muted)">Loading…</p>}
      {!error && races !== null && races.length === 0 && (
        <p className="text-sm text-(--color-muted)">No races ingested yet for {selectedYear}.</p>
      )}
      {!error && races !== null && races.length > 0 && (
        <ul className="divide-y divide-(--color-border) text-sm">
          {races.map((race) => (
            <li key={race.sessionKey} className="flex items-center justify-between gap-3 py-2">
              <div>
                <span className="flex flex-wrap items-center gap-2">
                  {race.featureRowCount > 0 ? (
                    <Link href={`/races/${race.sessionKey}`} className="font-medium text-(--color-accent) hover:underline">
                      {race.meetingName}
                    </Link>
                  ) : (
                    <span className="font-medium">{race.meetingName}</span>
                  )}
                  {race.isSprint && (
                    <RaceTag title="Sprint sessions score championship points but are never predicted">Sprint</RaceTag>
                  )}
                  {!race.isClassified && <RaceTag title="Results not yet published">Scheduled</RaceTag>}
                </span>
                <p className="text-(--color-muted)">{race.circuitShortName}, {race.countryName}</p>
              </div>
              <span className="shrink-0 text-(--color-muted)">{describeStatus(race)}</span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

/** Why a row has no predictions, or how many driver rows back them when it does. */
function describeStatus(race: SeasonRace): string {
  if (race.isSprint) return "Not predicted";
  if (!race.isClassified) return "Not yet run";
  return `${race.featureRowCount} feature rows`;
}

function RaceTag({ title, children }: { title: string; children: ReactNode }) {
  return (
    <span
      className="rounded-(--radius) bg-(--color-surface-hover) px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wider text-(--color-muted)"
      title={title}
    >
      {children}
    </span>
  );
}
