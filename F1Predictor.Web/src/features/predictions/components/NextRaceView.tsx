"use client";

import { useCallback } from "react";
import { getNextRacePreview } from "@/features/predictions/predictions-service";
import { PreviewTable } from "@/features/predictions/components/PreviewTable";
import { Card } from "@/shared/components/Card";
import { ErrorBanner } from "@/shared/components/ErrorBanner";
import { LiveFooter } from "@/shared/components/LiveFooter";
import { TableSkeleton } from "@/shared/components/Skeleton";
import { useLiveData } from "@/shared/lib/use-live-data";

interface NextRaceViewProps {
  year: number;
}

function countdown(to: Date): string {
  const ms = to.getTime() - Date.now();

  if (ms <= 0) return "under way";

  const hours = Math.floor(ms / 3_600_000);
  const days = Math.floor(hours / 24);

  if (days >= 1) return `in ${days} day${days === 1 ? "" : "s"}`;
  if (hours >= 1) return `in ${hours} hour${hours === 1 ? "" : "s"}`;

  return `in ${Math.max(1, Math.floor(ms / 60_000))} minutes`;
}

export function NextRaceView({ year }: NextRaceViewProps) {
  const fetcher = useCallback(() => getNextRacePreview(year), [year]);
  const { data, error, loading, refreshing, lastUpdated, refresh } = useLiveData(fetcher, [year]);

  if (loading) {
    return (
      <Card>
        <TableSkeleton rows={8} />
      </Card>
    );
  }

  if (error?.code === "Prediction.ModelsNotTrained") {
    return (
      <ErrorBanner
        title="Models not trained yet"
        message="Train the models for this season before previewing the next race."
        action={{ label: "Go train models", href: "/dashboard" }}
      />
    );
  }

  if (error?.code === "Prediction.NoUpcomingRace") {
    return (
      <ErrorBanner
        title="No upcoming race"
        message={`Every ${year} Grand Prix that has been ingested already has results. Re-ingest the season to pick up the rest of the calendar.`}
        action={{ label: "Go to dashboard", href: "/dashboard" }}
      />
    );
  }

  if (error?.code === "Prediction.NoEntryList") {
    return <ErrorBanner title="No entry list" message={error.userMessage} />;
  }

  if (error) {
    return <ErrorBanner title="Something went wrong" message={error.userMessage} />;
  }

  if (!data) {
    return null;
  }

  const start = new Date(data.dateStart);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold">{data.meetingName}</h1>
          <p className="text-sm text-(--color-muted)">
            {data.circuitShortName}, {data.countryName} · {start.toLocaleString()} ·{" "}
            {countdown(start)}
            {data.hasSprint && " · sprint weekend"}
          </p>
        </div>

        <span
          className={`rounded-(--radius) px-3 py-1 font-mono text-xs font-medium uppercase tracking-wider ${
            data.gridConfirmed
              ? "bg-(--color-points)/15 text-(--color-points)"
              : "bg-(--color-podium)/15 text-(--color-podium)"
          }`}
        >
          {data.gridConfirmed ? "Confirmed grid" : "Projected grid"}
        </span>
      </div>

      {!data.gridConfirmed && (
        <p className="rounded-(--radius) border border-(--color-border) bg-(--color-surface) p-3 text-sm text-(--color-muted)">
          Qualifying has not run yet, so the starting order below is projected from each driver&apos;s
          recent form rather than an actual grid. The models take grid position and gap to pole as
          their strongest inputs, so treat these probabilities as provisional — they will firm up on
          their own once the real grid is published.
        </p>
      )}

      <Card>
        <PreviewTable drivers={data.drivers} gridConfirmed={data.gridConfirmed} />
      </Card>

      <LiveFooter lastUpdated={lastUpdated} refreshing={refreshing} onRefresh={refresh} />
    </div>
  );
}
