import type { DriverPrediction } from "@/features/predictions/predictions-types";
import { TeamColour } from "@/shared/components/TeamColour";

interface PredictionsTableProps {
  drivers: DriverPrediction[];
}

function formatPercent(probability: number): string {
  return `${(probability * 100).toFixed(1)}%`;
}

export function PredictionsTable({ drivers }: PredictionsTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[640px] text-left text-sm">
        <thead>
          <tr className="border-b border-(--color-border) text-(--color-muted)">
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Driver</th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Team</th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Grid</th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Finish</th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Podium %</th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Points %</th>
            <th className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Actual podium</th>
            <th className="py-2 font-mono text-xs font-medium uppercase tracking-wider">Actual points</th>
          </tr>
        </thead>
        <tbody>
          {drivers.map((driver) => (
            <tr
              key={driver.driverNumber}
              className="border-b border-(--color-border) transition-colors last:border-0 hover:bg-(--color-surface-hover)"
            >
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
              <td className="py-2 pr-3 font-mono">{driver.actualPodium ? "✓" : "—"}</td>
              <td className="py-2 font-mono">{driver.actualPointsFinish ? "✓" : "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
