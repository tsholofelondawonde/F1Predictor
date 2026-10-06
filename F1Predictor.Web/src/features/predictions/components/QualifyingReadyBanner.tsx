"use client";

import { useEffect } from "react";
import { Button } from "@/shared/components/Button";
import { StatusAnnouncer } from "@/shared/components/StatusAnnouncer";
import { useIngestAndRetrain } from "@/shared/lib/use-ingest-and-retrain";

interface QualifyingReadyBannerProps {
  year: number;
  onIngested: () => void;
}

/**
 * Nudges the user that OpenF1 already has a grid the local database doesn't, with a one-click
 * shortcut for the Ingest -> Rebuild -> Train pipeline the background coordinator job would
 * otherwise get to on its own delay. Disappears on its own once the parent's gridConfirmed flag
 * catches up (it just stops being rendered), so no separate success message is needed here.
 */
export function QualifyingReadyBanner({ year, onIngested }: QualifyingReadyBannerProps) {
  const { status, stepLabel, error, run } = useIngestAndRetrain();
  const isRunning = status === "running";

  useEffect(() => {
    if (status === "success") {
      onIngested();
    }
  }, [status, onIngested]);

  return (
    <div className="rounded-(--radius) border border-(--color-podium)/40 border-l-4 bg-(--color-podium)/10 p-4 text-sm">
      <p className="font-mono text-xs font-semibold uppercase tracking-wider text-(--color-podium)">
        Qualifying has run
      </p>
      <p className="mt-1 text-(--color-foreground)">
        OpenF1 already has a starting grid for this race that hasn&apos;t been ingested yet.
      </p>
      <Button onClick={() => run(year)} disabled={isRunning} className="mt-3">
        {isRunning && stepLabel ? stepLabel : "Ingest & retrain"}
      </Button>

      <StatusAnnouncer
        message={
          status === "success"
            ? "Ingest and retrain complete."
            : status === "error" && error
              ? `${stepLabel ?? "Step"} failed: ${error.message}`
              : isRunning && stepLabel
                ? stepLabel
                : null
        }
      />

      {status === "error" && error && <p className="mt-2 text-sm text-(--color-error-text)">{error.message}</p>}
    </div>
  );
}
