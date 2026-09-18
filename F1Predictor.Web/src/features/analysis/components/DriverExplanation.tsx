"use client";

import { useEffect, useState } from "react";
import { explainDriver } from "@/features/analysis/analysis-service";
import type {
  DriverExplanationResponse,
  FeatureContribution,
  TargetExplanation,
} from "@/features/analysis/analysis-types";
import { formatProbability } from "@/shared/components/ProbabilityBar";
import { Skeleton } from "@/shared/components/Skeleton";
import { ApiError } from "@/shared/lib/api-error";
import { getErrorDisplay } from "@/shared/lib/error-display";

interface DriverExplanationProps {
  year: number;
  driverNumber: number;
}

/** Feature names as the model knows them, in reader's terms. */
const FEATURE_LABELS: Record<string, string> = {
  GridPosition: "Grid position",
  QualiGapToPole: "Gap to pole",
  PitStopCount: "Pit stops",
  AvgPitStopDuration: "Avg pit stop",
  Rainfall: "Rain",
};

/** The backend uses 99 as "set no qualifying time", which is a sentinel rather than a real gap. */
const MISSING_LAP_SENTINEL = 99;

function labelFor(feature: string): string {
  return FEATURE_LABELS[feature] ?? feature.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function formatValue(feature: string, value: number): string {
  if (feature === "GridPosition") return String(Math.round(value));
  if (feature === "QualiGapToPole") return value >= MISSING_LAP_SENTINEL ? "no time" : `+${value.toFixed(3)}s`;
  if (feature === "Rainfall") return value > 0 ? "yes" : "no";

  return value.toFixed(1);
}

function formatContribution(contribution: number): string {
  return `${contribution > 0 ? "+" : ""}${contribution.toFixed(2)}`;
}

function barColour(contribution: FeatureContribution): string {
  switch (contribution.direction) {
    case "helps":
      return "bg-(--color-points)";
    case "hurts":
      return "bg-(--color-accent)";
    default:
      return "bg-(--color-muted)/40";
  }
}

interface ContributionListProps {
  title: string;
  target: TargetExplanation;
}

function ContributionList({ title, target }: ContributionListProps) {
  const maxMagnitude = Math.max(...target.contributions.map((c) => Math.abs(c.contribution)), 0);

  return (
    <div>
      <h4 className="flex items-baseline justify-between font-mono text-xs font-semibold uppercase tracking-wider text-(--color-muted)">
        <span>{title}</span>
        <span className="tabular-nums text-(--color-foreground)">{formatProbability(target.probability)}</span>
      </h4>
      <ul className="mt-2 space-y-1.5">
        {target.contributions.map((contribution) => {
          const width = maxMagnitude > 0 ? (Math.abs(contribution.contribution) / maxMagnitude) * 100 : 0;

          return (
            <li key={contribution.feature} className="grid grid-cols-[minmax(0,7rem)_4rem_1fr_3.5rem] items-center gap-2 text-xs">
              <span className="truncate">{labelFor(contribution.feature)}</span>
              <span className="text-right font-mono tabular-nums text-(--color-muted)">
                {formatValue(contribution.feature, contribution.value)}
              </span>
              <span className="h-2 overflow-hidden rounded-(--radius-pill) bg-(--color-surface-hover)" aria-hidden="true">
                <span className={`block h-full ${barColour(contribution)}`} style={{ width: `${width}%` }} />
              </span>
              <span
                className="text-right font-mono tabular-nums"
                aria-label={`${contribution.direction}, ${formatContribution(contribution.contribution)}`}
              >
                {formatContribution(contribution.contribution)}
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

/**
 * Fetched once on mount, not polled: a row is expanded to read, and the numbers behind it only
 * move when the grid or the models do — the table above already refreshes on a timer.
 */
export function DriverExplanation({ year, driverNumber }: DriverExplanationProps) {
  const [data, setData] = useState<DriverExplanationResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    explainDriver(year, driverNumber)
      .then((response) => {
        if (!cancelled) setData(response);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setError(err instanceof ApiError ? getErrorDisplay(err).message : "Could not load this explanation.");
      });

    return () => {
      cancelled = true;
    };
  }, [year, driverNumber]);

  if (error) {
    return <p className="py-2 text-xs text-(--color-muted)">{error}</p>;
  }

  if (!data) {
    return (
      <div className="grid gap-4 py-2 sm:grid-cols-2" role="status" aria-label="Loading explanation">
        <Skeleton className="h-28 w-full" />
        <Skeleton className="h-28 w-full" />
      </div>
    );
  }

  return (
    <div className="py-2">
      <div className="grid gap-4 sm:grid-cols-2">
        <ContributionList title="Podium" target={data.podium} />
        <ContributionList title="Points" target={data.pointsFinish} />
      </div>

      {data.narrative && (
        <p className="mt-3 max-w-prose text-sm leading-relaxed">
          {data.narrative}
          {data.model && <span className="ml-2 font-mono text-xs text-(--color-muted)">— {data.model}</span>}
        </p>
      )}
    </div>
  );
}
