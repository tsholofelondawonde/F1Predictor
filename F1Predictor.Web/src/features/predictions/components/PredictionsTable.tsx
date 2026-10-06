"use client";

import { Fragment, useState, type ReactNode } from "react";
import type { DriverPrediction } from "@/features/predictions/predictions-types";
import { TeamColour } from "@/shared/components/TeamColour";

interface PredictionsTableProps {
  drivers: DriverPrediction[];
  /** When given, each row can expand to show why the models scored that driver as they did. */
  renderDetail?: (driverNumber: number) => ReactNode;
}

function formatPercent(probability: number): string {
  return `${(probability * 100).toFixed(1)}%`;
}

export function PredictionsTable({ drivers, renderDetail }: PredictionsTableProps) {
  const [expanded, setExpanded] = useState<ReadonlySet<number>>(new Set());
  const expandable = renderDetail !== undefined;
  const columnCount = expandable ? 9 : 8;

  const toggle = (driverNumber: number) =>
    setExpanded((current) => {
      const next = new Set(current);
      if (next.has(driverNumber)) next.delete(driverNumber);
      else next.add(driverNumber);
      return next;
    });

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[640px] text-left text-sm">
        <caption className="sr-only">Podium and points-finish predictions per driver</caption>
        <thead>
          <tr className="border-b border-(--color-border) text-(--color-muted)">
            {expandable && (
              <th scope="col" className="w-8 py-2 pr-1">
                <span className="sr-only">Explain</span>
              </th>
            )}
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Driver</th>
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Team</th>
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Grid</th>
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Finish</th>
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Podium %</th>
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Points %</th>
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Actual podium</th>
            <th scope="col" className="py-2 font-mono text-xs font-medium uppercase tracking-wider">Actual points</th>
          </tr>
        </thead>
        <tbody>
          {drivers.map((driver) => {
            const isExpanded = expandable && expanded.has(driver.driverNumber);
            const detailId = `driver-prediction-explanation-${driver.driverNumber}`;

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
                  <td className="py-2 pr-3">
                    <span className="flex items-center gap-2">
                      <TeamColour colour={driver.teamColour} title={driver.teamName} />
                      <span className="font-medium">{driver.fullName}</span>
                      <span className="font-mono text-xs text-(--color-muted)">{driver.driverNumber}</span>
                    </span>
                  </td>
                  <td className="py-2 pr-3 text-(--color-muted)">{driver.teamName}</td>
                  <td className="py-2 pr-3 font-mono tabular-nums">{driver.gridPosition}</td>
                  <td className="py-2 pr-3 font-mono tabular-nums">{driver.finishPosition}</td>
                  <td className="py-2 pr-3 font-mono tabular-nums text-(--color-podium)">{formatPercent(driver.podiumProbability)}</td>
                  <td className="py-2 pr-3 font-mono tabular-nums text-(--color-points)">{formatPercent(driver.pointsProbability)}</td>
                  <td className="py-2 pr-3 font-mono">
                    <span aria-hidden="true">{driver.actualPodium ? "✓" : "—"}</span>
                    <span className="sr-only">{driver.actualPodium ? "Yes" : "No"}</span>
                  </td>
                  <td className="py-2 font-mono">
                    <span aria-hidden="true">{driver.actualPointsFinish ? "✓" : "—"}</span>
                    <span className="sr-only">{driver.actualPointsFinish ? "Yes" : "No"}</span>
                  </td>
                </tr>
                {isExpanded && renderDetail !== undefined && (
                  <tr id={detailId} className="border-b border-(--color-border) bg-(--color-surface-hover)/40 last:border-0">
                    <td colSpan={columnCount} className="px-3 py-1">
                      {renderDetail(driver.driverNumber)}
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
