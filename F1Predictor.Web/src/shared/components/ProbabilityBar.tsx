interface ProbabilityBarProps {
  /** Probability in the range 0 to 1. */
  value: number;
  /** Bar colour. Defaults to the accent colour. */
  colour?: string;
  /** Dims the bar for contenders who can no longer win. */
  muted?: boolean;
  /** Renders as a blocky HUD-meter instead of a smooth pill — dashboard tables only. */
  segmented?: boolean;
}

const SEGMENT_COUNT = 10;

export function ProbabilityBar({ value, colour, muted = false, segmented = false }: ProbabilityBarProps) {
  const percent = Math.max(0, Math.min(1, value)) * 100;

  return (
    <div className="flex items-center gap-2">
      {segmented ? (
        <div
          className="grid min-w-16 flex-1 grid-cols-10 gap-0.5"
          role="meter"
          aria-valuenow={Number(percent.toFixed(1))}
          aria-valuemin={0}
          aria-valuemax={100}
        >
          {Array.from({ length: SEGMENT_COUNT }, (_, index) => {
            const filled = index < Math.round((percent / 100) * SEGMENT_COUNT);

            return (
              <span
                key={index}
                aria-hidden="true"
                className="h-2.5 transition-colors duration-300"
                style={{
                  backgroundColor: filled ? (colour ?? "var(--color-accent)") : "var(--color-surface-hover)",
                  opacity: filled && muted ? 0.35 : 1,
                }}
              />
            );
          })}
        </div>
      ) : (
        <div
          className="h-2 w-full min-w-16 overflow-hidden rounded-(--radius-pill) bg-(--color-surface-hover)"
          role="meter"
          aria-valuenow={Number(percent.toFixed(1))}
          aria-valuemin={0}
          aria-valuemax={100}
        >
          <div
            className="h-full rounded-(--radius-pill) transition-[width] duration-500"
            style={{
              width: `${percent}%`,
              backgroundColor: colour ?? "var(--color-accent)",
              opacity: muted ? 0.35 : 1,
            }}
          />
        </div>
      )}
      <span className="w-14 shrink-0 text-right font-mono text-xs tabular-nums">
        {formatProbability(value)}
      </span>
    </div>
  );
}

/**
 * Rounding a long shot to "0.0%" reads as impossible when it is not, so anything
 * that happened at all in the simulation gets "<0.1%" instead.
 */
export function formatProbability(value: number): string {
  if (value <= 0) return "0%";
  if (value < 0.001) return "<0.1%";

  return `${(value * 100).toFixed(1)}%`;
}
