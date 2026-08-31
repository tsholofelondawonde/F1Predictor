"use client";

import { Button } from "@/shared/components/Button";

interface LiveFooterProps {
  lastUpdated: Date | null;
  refreshing: boolean;
  onRefresh: () => void;
}

/**
 * Says when the data was last read and offers a manual re-read.
 *
 * Worth showing rather than silently polling: "live" here means the page re-reads the API every
 * minute, not that it is streaming a running session, and a visible timestamp is the honest way
 * to say so.
 */
export function LiveFooter({ lastUpdated, refreshing, onRefresh }: LiveFooterProps) {
  return (
    <div className="flex items-center justify-between gap-3 border-t border-(--color-border) pt-3 font-mono text-xs uppercase tracking-wider text-(--color-muted)">
      <span aria-live="polite" className="flex items-center gap-2">
        <span
          aria-hidden="true"
          className={`h-1.5 w-1.5 rounded-(--radius-pill) bg-(--color-terminal-green) ${
            refreshing ? "animate-pulse" : ""
          }`}
        />
        {refreshing
          ? "Syncing…"
          : lastUpdated
            ? `Last sync ${lastUpdated.toLocaleTimeString()} · refreshes every minute`
            : "Not loaded yet"}
      </span>
      <Button variant="secondary" onClick={onRefresh} disabled={refreshing} className="px-3 py-1 text-xs uppercase tracking-wider">
        Refresh now
      </Button>
    </div>
  );
}
