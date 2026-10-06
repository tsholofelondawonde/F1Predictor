"use client";

import { useSeasonsStore } from "@/features/seasons/seasons-store";
import { refreshAnalysis } from "@/features/analysis/analysis-service";
import { Button } from "@/shared/components/Button";
import { Card } from "@/shared/components/Card";
import { StatusAnnouncer } from "@/shared/components/StatusAnnouncer";
import { useAsyncAction } from "@/shared/lib/use-async-action";

export function AnalysisRefreshButton() {
  const selectedYear = useSeasonsStore((state) => state.selectedYear);
  const { status, result, error, run } = useAsyncAction(refreshAnalysis);

  return (
    <Card title="4. Refresh AI preview">
      <p className="mb-3 text-sm text-(--color-muted)">
        Regenerates the selected season&apos;s next-race preview if it is missing or stale — a real grid
        replacing a projected one, or a newer classified race. This is the same refresh the background job
        runs automatically after ingestion; use this to force it on demand. Does nothing if no AI provider
        is configured.
      </p>
      <Button onClick={() => run(selectedYear)} disabled={status === "running"} variant="secondary">
        {status === "running" ? "Refreshing…" : `Refresh ${selectedYear} preview`}
      </Button>

      <StatusAnnouncer
        message={
          status === "success" && result
            ? `Refresh complete: ${result.previewsGenerated} preview(s) generated, ${result.skipped} skipped.`
            : status === "error" && error
              ? `Refresh failed: ${error.message}`
              : null
        }
      />

      {status === "error" && error && <p className="mt-3 text-sm text-(--color-error-text)">{error.message}</p>}

      {status === "success" && result && (
        <div className="mt-3 text-sm text-(--color-muted)">
          <p>
            {result.previewsGenerated} preview(s) generated, {result.skipped} skipped.
          </p>
          {result.notes.length > 0 && (
            <ul className="mt-2 list-inside list-disc">
              {result.notes.map((note) => (
                <li key={note}>{note}</li>
              ))}
            </ul>
          )}
        </div>
      )}
    </Card>
  );
}
