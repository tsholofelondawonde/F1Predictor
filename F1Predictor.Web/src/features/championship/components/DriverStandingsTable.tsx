import type { DriverForecast } from "@/features/championship/championship-types";
import { ProbabilityBar } from "@/shared/components/ProbabilityBar";
import { TeamColour, teamColourCss } from "@/shared/components/TeamColour";

interface DriverStandingsTableProps {
  drivers: DriverForecast[];
}

export function DriverStandingsTable({ drivers }: DriverStandingsTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[720px] text-left text-sm">
        <thead>
          <tr className="border-b border-(--color-border) text-(--color-muted)">
            <th className="py-2 pr-3 font-medium">#</th>
            <th className="py-2 pr-3 font-medium">Driver</th>
            <th className="py-2 pr-3 font-medium">Team</th>
            <th className="py-2 pr-3 text-right font-medium">Pts</th>
            <th className="py-2 pr-3 text-right font-medium">Wins</th>
            <th className="py-2 pr-3 font-medium">Title chance</th>
            <th className="py-2 font-medium">Projected</th>
          </tr>
        </thead>
        <tbody>
          {drivers.map((driver) => (
            <tr
              key={driver.driverNumber}
              className={`border-b border-(--color-border) last:border-0 ${
                driver.isMathematicallyAlive ? "" : "text-(--color-muted)"
              }`}
            >
              <td className="py-2 pr-3 font-mono tabular-nums">{driver.position}</td>
              <td className="py-2 pr-3">
                <span className="flex items-center gap-2">
                  <TeamColour colour={driver.teamColour} title={driver.teamName} />
                  <span className="font-medium">{driver.fullName}</span>
                  <span className="font-mono text-xs text-(--color-muted)">{driver.driverNumber}</span>
                </span>
              </td>
              <td className="py-2 pr-3 text-(--color-muted)">{driver.teamName}</td>
              <td className="py-2 pr-3 text-right font-mono tabular-nums">{driver.points}</td>
              <td className="py-2 pr-3 text-right font-mono tabular-nums">{driver.wins}</td>
              <td className="py-2 pr-3">
                <ProbabilityBar
                  value={driver.titleProbability}
                  colour={teamColourCss(driver.teamColour)}
                  muted={!driver.isMathematicallyAlive}
                />
              </td>
              <td className="py-2 font-mono text-xs tabular-nums text-(--color-muted)">
                {Math.round(driver.projectedPoints)}
                <span className="ml-1 opacity-70">
                  ({Math.round(driver.projectedPointsLow)}–{Math.round(driver.projectedPointsHigh)})
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
