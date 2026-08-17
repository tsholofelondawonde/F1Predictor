"use client";

import { useCallback } from "react";
import { getTitleScenarios } from "@/features/championship/championship-service";
import type { TitleScenario } from "@/features/championship/championship-types";
import { Card } from "@/shared/components/Card";
import { ErrorBanner } from "@/shared/components/ErrorBanner";
import { LiveFooter } from "@/shared/components/LiveFooter";
import { formatProbability, ProbabilityBar } from "@/shared/components/ProbabilityBar";
import { TeamColour, teamColourCss } from "@/shared/components/TeamColour";
import { useLiveData } from "@/shared/lib/use-live-data";

interface ScenariosViewProps {
  year: number;
}

function statusChip(scenario: TitleScenario) {
  if (scenario.hasClinchedTitle) return { label: "Champion", tone: "podium" as const };
  if (scenario.isLeader) return { label: "Leader", tone: "podium" as const };
  if (!scenario.isMathematicallyAlive) return { label: "Eliminated", tone: "muted" as const };

  return { label: "Still alive", tone: "points" as const };
}

const TONE_CLASS = {
  podium: "bg-(--color-podium)/15 text-(--color-podium)",
  points: "bg-(--color-points)/15 text-(--color-points)",
  muted: "bg-(--color-surface-hover) text-(--color-muted)",
} as const;

export function ScenariosView({ year }: ScenariosViewProps) {
  const fetcher = useCallback(() => getTitleScenarios(year), [year]);
  const { data, error, loading, refreshing, lastUpdated, refresh } = useLiveData(fetcher, [year]);

  if (loading) {
    return <p className="text-sm text-(--color-muted)">Working out the permutations…</p>;
  }

  if (error?.code === "Championship.NoResults") {
    return (
      <ErrorBanner
        title="No results yet"
        message={`Nothing has been ingested for ${year}. Ingest the season first.`}
        action={{ label: "Go to dashboard", href: "/" }}
      />
    );
  }

  if (error?.code === "Championship.SeasonComplete") {
    return (
      <ErrorBanner
        title="Season complete"
        message={`Every session of ${year} has been run — the championship is already decided.`}
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
      <div>
        <h1 className="text-xl font-semibold">What has to happen</h1>
        <p className="text-sm text-(--color-muted)">
          {data.leaderName} leads with {data.racesRemaining} race
          {data.racesRemaining === 1 ? "" : "s"}
          {data.sprintsRemaining > 0 &&
            ` and ${data.sprintsRemaining} sprint${data.sprintsRemaining === 1 ? "" : "s"}`}{" "}
          left, so no driver can score more than {data.pointsStillAvailable} further points.
        </p>
      </div>

      <div className="space-y-3">
        {data.contenders.map((scenario) => {
          const chip = statusChip(scenario);

          return (
            <Card key={scenario.driverNumber}>
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="flex items-center gap-2">
                  <span className="font-mono text-sm text-(--color-muted)">{scenario.position}.</span>
                  <TeamColour colour={scenario.teamColour} title={scenario.teamName} />
                  <div>
                    <p className="font-medium">{scenario.fullName}</p>
                    <p className="text-xs text-(--color-muted)">{scenario.teamName}</p>
                  </div>
                </div>

                <span className={`rounded-full px-2.5 py-1 text-xs font-medium ${TONE_CLASS[chip.tone]}`}>
                  {chip.label}
                </span>
              </div>

              <p className="mt-3 text-sm">{scenario.requirement}</p>

              <dl className="mt-3 grid grid-cols-2 gap-3 text-xs sm:grid-cols-4">
                <Stat label="Points" value={String(scenario.points)} />
                <Stat
                  label="Behind leader"
                  value={scenario.isLeader ? "—" : `${scenario.pointsBehindLeader}`}
                />
                <Stat label="Best possible" value={String(scenario.maximumPossiblePoints)} />
                <Stat
                  label="Needs per race"
                  value={
                    scenario.requiredPointsPerRace === null
                      ? "—"
                      : scenario.requiredPointsPerRace > 25
                        ? "more than a win"
                        : `${scenario.requiredPointsPerRace.toFixed(1)} pts`
                  }
                />
              </dl>

              <div className="mt-3 flex items-center gap-3">
                <span className="text-xs text-(--color-muted)">Simulated title chance</span>
                <div className="max-w-56 flex-1">
                  <ProbabilityBar
                    value={scenario.titleProbability}
                    colour={teamColourCss(scenario.teamColour)}
                    muted={!scenario.isMathematicallyAlive}
                  />
                </div>
                <span className="sr-only">{formatProbability(scenario.titleProbability)}</span>
              </div>
            </Card>
          );
        })}
      </div>

      <LiveFooter lastUpdated={lastUpdated} refreshing={refreshing} onRefresh={refresh} />
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-(--color-muted)">{label}</dt>
      <dd className="mt-0.5 font-mono tabular-nums">{value}</dd>
    </div>
  );
}
