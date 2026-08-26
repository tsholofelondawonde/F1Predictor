import type { PreviewDriver } from "@/features/predictions/predictions-types";
import { ProbabilityBar } from "@/shared/components/ProbabilityBar";
import { TeamColour, teamColourCss } from "@/shared/components/TeamColour";

interface PreviewTableProps {
  drivers: PreviewDriver[];
  gridConfirmed: boolean;
}

/** The backend uses 99 as "set no qualifying time", which is a sentinel rather than a real gap. */
const MISSING_LAP_SENTINEL = 99;

function formatGap(gap: number): string {
  return gap >= MISSING_LAP_SENTINEL ? "no time" : `+${gap.toFixed(3)}s`;
}

export function PreviewTable({ drivers, gridConfirmed }: PreviewTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[680px] text-left text-sm">
        <thead>
          <tr className="border-b border-(--color-border) text-(--color-muted)">
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
          {drivers.map((driver) => (
            <tr
              key={driver.driverNumber}
              className="border-b border-(--color-border) transition-colors last:border-0 hover:bg-(--color-surface-hover)"
            >
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
          ))}
        </tbody>
      </table>
    </div>
  );
}
