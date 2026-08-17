interface ProbabilityBarProps {
  /** Probability in the range 0 to 1. */
  value: number;
  /** Bar colour. Defaults to the accent colour. */
  colour?: string;
  /** Dims the bar for contenders who can no longer win. */
  muted?: boolean;
}

export function ProbabilityBar({ value, colour, muted = false }: ProbabilityBarProps) {
  const percent = Math.max(0, Math.min(1, value)) * 100;

  return (
    <div className="flex items-center gap-2">
      <div
        className="h-2 w-full min-w-16 overflow-hidden rounded-full bg-(--color-surface-hover)"
        role="meter"
        aria-valuenow={Number(percent.toFixed(1))}
        aria-valuemin={0}
        aria-valuemax={100}
      >
        <div
          className="h-full rounded-full transition-[width] duration-500"
          style={{
            width: `${percent}%`,
            backgroundColor: colour ?? "var(--color-accent)",
            opacity: muted ? 0.35 : 1,
          }}
        />
      </div>
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
