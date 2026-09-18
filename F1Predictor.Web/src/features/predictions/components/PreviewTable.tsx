"use client";

import { Fragment, useState } from "react";
import { DriverExplanation } from "@/features/analysis/components/DriverExplanation";
import type { PreviewDriver } from "@/features/predictions/predictions-types";
import { ProbabilityBar } from "@/shared/components/ProbabilityBar";
import { TeamColour, teamColourCss } from "@/shared/components/TeamColour";

interface PreviewTableProps {
  drivers: PreviewDriver[];
  gridConfirmed: boolean;
  /** When given, each row can expand to show why the models scored that driver as they did. */
  year?: number;
}

/** The backend uses 99 as "set no qualifying time", which is a sentinel rather than a real gap. */
const MISSING_LAP_SENTINEL = 99;

function formatGap(gap: number): string {
  return gap >= MISSING_LAP_SENTINEL ? "no time" : `+${gap.toFixed(3)}s`;
}

export function PreviewTable({ drivers, gridConfirmed, year }: PreviewTableProps) {
  const [expanded, setExpanded] = useState<ReadonlySet<number>>(new Set());
  const expandable = year !== undefined;
  const columnCount = expandable ? 7 : 6;

  const toggle = (driverNumber: number) =>
    setExpanded((current) => {
      const next = new Set(current);
      if (next.has(driverNumber)) next.delete(driverNumber);
      else next.add(driverNumber);
      return next;
    });

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[680px] text-left text-sm">
        <thead>
          <tr className="border-b border-(--color-border) text-(--color-muted)">
            {expandable && (
              <th className="w-8 py-2 pr-1">
                <span className="sr-only">Explain</span>
              </th>
            )}
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">
              {gridConfirmed ? "Grid" : "Proj. grid"}
            </th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Driver</th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Team</th>
            <th className="py-2 pr-3 text-right font-mono text-xs font-medium uppercase tracking-wider">
              Gap to pole
            </th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Podium</th>
            <th className="py-2 font-mono text-xs font-medium uppercase tracking-wider">Points</th>
          </tr>
        </thead>
        <tbody>
          {drivers.map((driver) => {
            const isExpanded = expandable && expanded.has(driver.driverNumber);
            const detailId = `driver-explanation-${driver.driverNumber}`;

            return (
              <Fragment key={driver.driverNumber}>
                <tr
                  className={`border-b border-(--color-border) transition-colors last:border-0 hover:bg-(--color-surface-hover) ${
                    isExpanded ? "border-b-0" : ""
                  }`}
                >
                  {expandable && (
                    <td className="py-2 pr-1">
                      <button
                        type="button"
                        onClick={() => toggle(driver.driverNumber)}
                        aria-expanded={isExpanded}
                        aria-controls={detailId}
                        aria-label={`${isExpanded ? "Hide" : "Show"} why the models scored ${driver.fullName} this way`}
                        className="flex h-6 w-6 items-center justify-center rounded-(--radius) text-(--color-muted) transition-colors hover:bg-(--color-surface-hover) hover:text-(--color-foreground) focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent)"
                      >
                        <svg
                          aria-hidden="true"
                          viewBox="0 0 16 16"
                          className={`h-3.5 w-3.5 transition-transform ${isExpanded ? "rotate-90" : ""}`}
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        >
                          <path d="M6 3l5 5-5 5" />
                        </svg>
                      </button>
                    </td>
                  )}
                  <td className="py-2 pr-3 font-mono tabular-nums">{driver.gridPosition}</td>
                  <td className="py-2 pr-3">
                    <span className="flex items-center gap-2">
                      <TeamColour colour={driver.teamColour} title={driver.teamName} />
                      <span className="font-medium">{driver.fullName}</span>
                      {!driver.hasForm && (
                        <span
                          className="rounded-(--radius) bg-(--color-surface-hover) px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wider text-(--color-muted)"
                          title="No finishes this season, so there is nothing to project from"
                        >
                          no form
                        </span>
                      )}
                    </span>
                  </td>
                  <td className="py-2 pr-3 text-(--color-muted)">{driver.teamName}</td>
                  <td className="py-2 pr-3 text-right font-mono text-xs tabular-nums text-(--color-muted)">
                    {formatGap(driver.qualiGapToPole)}
                  </td>
                  <td className="py-2 pr-3">
                    <ProbabilityBar
                      value={driver.podiumProbability}
                      colour={teamColourCss(driver.teamColour)}
                      segmented
                    />
                  </td>
                  <td className="py-2">
                    <ProbabilityBar value={driver.pointsProbability} colour="var(--color-points)" segmented />
                  </td>
                </tr>
                {isExpanded && year !== undefined && (
                  <tr id={detailId} className="border-b border-(--color-border) bg-(--color-surface-hover)/40 last:border-0">
                    <td colSpan={columnCount} className="px-3 py-1">
                      <DriverExplanation year={year} driverNumber={driver.driverNumber} />
                    </td>
                  </tr>
                )}
              </Fragment>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
