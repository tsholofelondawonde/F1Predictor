"use client";

import { useState } from "react";
import { rebuildFeatures } from "@/features/seasons/seasons-service";
import type { RebuildFeaturesResponse } from "@/features/seasons/seasons-types";
import { Button } from "@/shared/components/Button";
import { Card } from "@/shared/components/Card";
import { ApiError } from "@/shared/lib/api-error";

export function RebuildFeaturesButton() {
  const [status, setStatus] = useState<"idle" | "running" | "success" | "error">("idle");
  const [result, setResult] = useState<RebuildFeaturesResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleRebuild() {
    setStatus("running");
    setError(null);
    try {
      const response = await rebuildFeatures();
      setResult(response);
      setStatus("success");
    } catch (err) {
      setError(err instanceof ApiError ? err.userMessage : "Rebuild failed unexpectedly.");
      setStatus("error");
    }
  }

  return (
    <Card title="2. Rebuild features">
      <p className="mb-3 text-sm text-(--color-muted)">
        Clears and regenerates the feature table from ingested race data. Cheap to re-run.
      </p>
      <Button onClick={handleRebuild} disabled={status === "running"} variant="secondary">
        {status === "running" ? "Rebuilding…" : "Rebuild features"}
      </Button>
      {status === "error" && error && <p className="mt-3 text-sm text-(--color-error-text)">{error}</p>}
      {status === "success" && result && (
        <p className="mt-3 text-sm text-(--color-muted)">
          {result.featureRows} feature rows across {result.racesWithFeatures} races
          {result.racesSkipped > 0 ? ` (${result.racesSkipped} skipped)` : ""}.
        </p>
      )}
    </Card>
  );
}
