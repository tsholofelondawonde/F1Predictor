"use client";

import { rebuildFeatures } from "@/features/seasons/seasons-service";
import { Button } from "@/shared/components/Button";
import { Card } from "@/shared/components/Card";
import { StatusAnnouncer } from "@/shared/components/StatusAnnouncer";
import { useAsyncAction } from "@/shared/lib/use-async-action";

export function RebuildFeaturesButton() {
  const { status, result, error, run } = useAsyncAction(rebuildFeatures);

  return (
    <Card title="2. Rebuild features">
      <p className="mb-3 text-sm text-(--color-muted)">
        Clears and regenerates the feature table from ingested race data. Cheap to re-run.
      </p>
      <Button onClick={() => run()} disabled={status === "running"} variant="secondary">
        {status === "running" ? "Rebuilding…" : "Rebuild features"}
      </Button>
      <StatusAnnouncer
        message={
          status === "success" && result
            ? `Rebuild complete: ${result.featureRows} feature rows across ${result.racesWithFeatures} races.`
            : status === "error" && error
              ? `Rebuild failed: ${error.message}`
              : null
        }
      />
      {status === "error" && error && <p className="mt-3 text-sm text-(--color-error-text)">{error.message}</p>}
      {status === "success" && result && (
        <p className="mt-3 text-sm text-(--color-muted)">
          {result.featureRows} feature rows across {result.racesWithFeatures} races
          {result.racesSkipped > 0 ? ` (${result.racesSkipped} skipped)` : ""}.
        </p>
      )}
    </Card>
  );
}
