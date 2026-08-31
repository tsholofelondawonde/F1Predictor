"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
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
            <li key={race.sessionKey} className="flex items-center justify-between py-2">
              <div>
                <Link href={`/races/${race.sessionKey}`} className="font-medium text-(--color-accent) hover:underline">
                  {race.meetingName}
                </Link>
                <p className="text-(--color-muted)">{race.circuitShortName}, {race.countryName}</p>
              </div>
              <span className="text-(--color-muted)">{race.featureRowCount} feature rows</span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
