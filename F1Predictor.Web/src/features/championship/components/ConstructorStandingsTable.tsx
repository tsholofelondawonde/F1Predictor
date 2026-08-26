import type { ConstructorForecast } from "@/features/championship/championship-types";
import { ProbabilityBar } from "@/shared/components/ProbabilityBar";
import { TeamColour, teamColourCss } from "@/shared/components/TeamColour";

interface ConstructorStandingsTableProps {
  constructors: ConstructorForecast[];
}

export function ConstructorStandingsTable({ constructors }: ConstructorStandingsTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[640px] text-left text-sm">
        <caption className="sr-only">Constructors&apos; championship standings and title chances</caption>
        <thead>
          <tr className="border-b border-(--color-border) text-(--color-muted)">
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">#</th>
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Constructor</th>
            <th scope="col" className="py-2 pr-3 text-right font-mono text-xs font-medium uppercase tracking-wider">Pts</th>
            <th scope="col" className="py-2 pr-3 text-right font-mono text-xs font-medium uppercase tracking-wider">Wins</th>
            <th scope="col" className="py-2 pr-3 font-mono text-xs font-medium uppercase tracking-wider">Title chance</th>
            <th scope="col" className="py-2 font-mono text-xs font-medium uppercase tracking-wider">Projected</th>
          </tr>
        </thead>
        <tbody>
          {constructors.map((team) => (
            <tr
              key={team.teamName}
              className={`border-b border-(--color-border) transition-colors last:border-0 hover:bg-(--color-surface-hover) ${
                team.isMathematicallyAlive ? "" : "text-(--color-muted)"
              }`}
            >
              <td className="py-2 pr-3 font-mono tabular-nums">{team.position}</td>
              <td className="py-2 pr-3">
                <span className="flex items-center gap-2">
                  <TeamColour colour={team.teamColour} title={team.teamName} />
                  <span className="font-medium">{team.teamName}</span>
                </span>
              </td>
              <td className="py-2 pr-3 text-right font-mono tabular-nums">{team.points}</td>
              <td className="py-2 pr-3 text-right font-mono tabular-nums">{team.wins}</td>
              <td className="py-2 pr-3">
                <ProbabilityBar
                  value={team.titleProbability}
                  colour={teamColourCss(team.teamColour)}
                  muted={!team.isMathematicallyAlive}
                  segmented
                />
              </td>
              <td className="py-2 font-mono text-xs tabular-nums text-(--color-muted)">
                {Math.round(team.projectedPoints)}
                <span className="ml-1 opacity-70">
                  ({Math.round(team.projectedPointsLow)}–{Math.round(team.projectedPointsHigh)})
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
