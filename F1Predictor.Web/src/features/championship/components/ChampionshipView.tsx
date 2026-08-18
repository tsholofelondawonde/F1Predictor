"use client";

import { useCallback, useState } from "react";
import { getChampionshipForecast } from "@/features/championship/championship-service";
import { DriverStandingsTable } from "@/features/championship/components/DriverStandingsTable";
import { ConstructorStandingsTable } from "@/features/championship/components/ConstructorStandingsTable";
import { LiveFooter } from "@/shared/components/LiveFooter";
import { Card } from "@/shared/components/Card";
import { ErrorBanner } from "@/shared/components/ErrorBanner";
import { TableSkeleton } from "@/shared/components/Skeleton";
import { useLiveData } from "@/shared/lib/use-live-data";

interface ChampionshipViewProps {
  year: number;
}

type Table = "drivers" | "constructors";

export function ChampionshipView({ year }: ChampionshipViewProps) {
  const [table, setTable] = useState<Table>("drivers");

  const fetcher = useCallback(() => getChampionshipForecast(year), [year]);
  const { data, error, loading, refreshing, lastUpdated, refresh } = useLiveData(fetcher, [year]);

  if (loading) {
    return (
      <Card>
        <TableSkeleton rows={10} />
      </Card>
    );
  }

  if (error?.code === "Championship.NoResults") {
    return (
      <ErrorBanner
        title="No results yet"
        message={`Nothing has been ingested for ${year}. Ingest the season first.`}
        action={{ label: "Go to dashboard", href: "/dashboard" }}
      />
    );
  }

  if (error?.code === "Championship.SeasonComplete") {
    return (
      <ErrorBanner
        title="Season complete"
        message={`Every session of ${year} has been run, so there is nothing left to forecast.`}
      />
    );
  }

  if (error) {
    return <ErrorBanner title="Something went wrong" message={error.userMessage} />;
  }

  if (!data) {
    return null;
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold">{data.year} Championship</h1>
          <p className="text-sm text-(--color-muted)">
            {data.racesRemaining} race{data.racesRemaining === 1 ? "" : "s"}
            {data.sprintsRemaining > 0 &&
              ` and ${data.sprintsRemaining} sprint${data.sprintsRemaining === 1 ? "" : "s"}`}{" "}
            still to run — up to {data.pointsStillAvailable} points for any one driver.
          </p>
        </div>

        <div
          className="flex rounded-(--radius) border border-(--color-border) p-0.5 font-mono text-xs uppercase tracking-wider"
          role="tablist"
          aria-label="Championship table"
        >
          {(["drivers", "constructors"] as const).map((option) => (
            <button
              key={option}
              type="button"
              role="tab"
              aria-selected={table === option}
              onClick={() => setTable(option)}
              className={`rounded-(--radius) px-3 py-1 transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent) ${
                table === option
                  ? "bg-(--color-accent) text-(--color-on-accent)"
                  : "text-(--color-muted) hover:text-(--color-foreground)"
              }`}
            >
              {option}
            </button>
          ))}
        </div>
      </div>

      <Card>
        {table === "drivers" ? (
          <DriverStandingsTable drivers={data.drivers} />
        ) : (
          <ConstructorStandingsTable constructors={data.constructors} />
        )}
      </Card>

      <details className="rounded-(--radius) border border-(--color-border) p-4 text-sm">
        <summary className="cursor-pointer font-medium focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent)">
          How these title chances are worked out
        </summary>
        <p className="mt-2 text-(--color-muted)">{data.method}</p>
        <p className="mt-2 text-(--color-muted)">
          Based on {data.simulations.toLocaleString()} simulated seasons with a fixed seed, so the
          numbers only move when new results arrive.
        </p>
      </details>

      <LiveFooter lastUpdated={lastUpdated} refreshing={refreshing} onRefresh={refresh} />
    </div>
  );
}
