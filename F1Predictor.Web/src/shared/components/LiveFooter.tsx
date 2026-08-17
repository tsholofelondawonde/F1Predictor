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
    <div className="flex items-center justify-between gap-3 text-xs text-(--color-muted)">
      <span aria-live="polite">
        {refreshing
          ? "Refreshing…"
          : lastUpdated
            ? `Updated ${lastUpdated.toLocaleTimeString()} · refreshes every minute`
            : "Not loaded yet"}
      </span>
      <Button variant="secondary" onClick={onRefresh} disabled={refreshing} className="px-3 py-1 text-xs">
        Refresh now
      </Button>
    </div>
  );
}
