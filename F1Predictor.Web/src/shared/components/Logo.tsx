/**
 * GridMind brand mark — a probability curve rising across a starting-grid field
 * to a committed apex node. Body strokes use `currentColor` so the mark inverts
 * with its context; the apex dot is always the brand accent.
 */
export function Logo({ className }: { className?: string }) {
  const dots = [6, 13, 20, 26];
  return (
    <svg
      viewBox="0 0 32 32"
      className={className}
      fill="none"
      aria-hidden="true"
      focusable="false"
    >
      <g fill="currentColor" opacity={0.18}>
        {dots.flatMap((x) =>
          dots.map((y) => <circle key={`${x}-${y}`} cx={x} cy={y} r={0.9} />),
        )}
      </g>
      <path
        d="M4 27C12 26 16 21 20 13S25 6 28 5"
        stroke="currentColor"
        strokeWidth={2.6}
        strokeLinecap="round"
      />
      <circle cx={4} cy={27} r={1.8} fill="currentColor" />
      <circle cx={28} cy={5} r={3} fill="var(--color-accent, #ef4444)" />
    </svg>
  );
}
