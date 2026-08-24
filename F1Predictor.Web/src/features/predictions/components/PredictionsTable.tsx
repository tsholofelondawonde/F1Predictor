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
        <caption className="sr-only">Podium and points-finish predictions per driver</caption>
        <thead>
          <tr className="border-b border-(--color-border) text-(--color-muted)">
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
              <td className="py-2 pr-3 font-mono">
                <span aria-hidden="true">{driver.actualPodium ? "✓" : "—"}</span>
                <span className="sr-only">{driver.actualPodium ? "Yes" : "No"}</span>
              </td>
              <td className="py-2 font-mono">
                <span aria-hidden="true">{driver.actualPointsFinish ? "✓" : "—"}</span>
                <span className="sr-only">{driver.actualPointsFinish ? "Yes" : "No"}</span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
