interface TeamColourProps {
  /** Six-digit hex from OpenF1, with no leading hash. Blank for drivers with no entry list. */
  colour: string;
  title?: string;
}

/** OpenF1 gives colours without the hash, and leaves them blank often enough to need a fallback. */
export function teamColourCss(colour: string): string {
  return /^[0-9a-f]{6}$/i.test(colour) ? `#${colour}` : "var(--color-muted)";
}

export function TeamColour({ colour, title }: TeamColourProps) {
  return (
    <span
      aria-hidden="true"
      title={title}
      className="inline-block h-4 w-1 shrink-0 rounded-full align-middle"
      style={{ backgroundColor: teamColourCss(colour) }}
    />
  );
}
