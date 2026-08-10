import type { DriverPrediction } from "@/features/predictions/predictions-types";

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
            <th className="py-2 pr-3 font-medium">Driver #</th>
            <th className="py-2 pr-3 font-medium">Grid</th>
            <th className="py-2 pr-3 font-medium">Finish</th>
            <th className="py-2 pr-3 font-medium">Podium %</th>
            <th className="py-2 pr-3 font-medium">Points %</th>
            <th className="py-2 pr-3 font-medium">Actual podium</th>
            <th className="py-2 font-medium">Actual points</th>
          </tr>
        </thead>
        <tbody>
          {drivers.map((driver) => (
            <tr key={driver.driverNumber} className="border-b border-(--color-border) last:border-0">
              <td className="py-2 pr-3 font-mono">{driver.driverNumber}</td>
              <td className="py-2 pr-3">{driver.gridPosition}</td>
              <td className="py-2 pr-3">{driver.finishPosition}</td>
              <td className="py-2 pr-3 font-mono text-(--color-podium)">{formatPercent(driver.podiumProbability)}</td>
              <td className="py-2 pr-3 font-mono text-(--color-points)">{formatPercent(driver.pointsProbability)}</td>
              <td className="py-2 pr-3">{driver.actualPodium ? "✓" : "–"}</td>
              <td className="py-2">{driver.actualPointsFinish ? "✓" : "–"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
