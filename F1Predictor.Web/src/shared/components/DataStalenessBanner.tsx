import type { DataStatus } from "@/features/seasons/seasons-types";

interface DataStalenessBannerProps {
  status: DataStatus | null;
}

/**
 * Nudges the user back to the dashboard's Ingest → Rebuild → Train panels when the pipeline has
 * fallen behind — not an error, so it uses `--color-podium` rather than the error palette and
 * never replaces the page's own content.
 */
export function DataStalenessBanner({ status }: DataStalenessBannerProps) {
  if (!status?.isStale) {
    return null;
  }

  const message = status.pendingRaceName
    ? `${status.pendingRaceName} (${new Date(status.pendingRaceDate!).toLocaleDateString()}) has been raced but hasn't been ingested yet.`
    : status.featuresStale
      ? "New results are ingested but features haven't been rebuilt yet."
      : "Models haven't been trained yet, so predictions may be unavailable or out of date.";

  return (
    <div className="rounded-(--radius) border border-(--color-podium)/40 border-l-4 bg-(--color-podium)/10 p-4 text-sm">
      <p className="font-mono text-xs font-semibold uppercase tracking-wider text-(--color-podium)">
        Data may be out of date
      </p>
      <p className="mt-1 text-(--color-foreground)">{message}</p>
      <a
        href="/dashboard"
        className="mt-2 inline-block font-medium text-(--color-accent) underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent)"
      >
        Go to dashboard
      </a>
    </div>
  );
}
