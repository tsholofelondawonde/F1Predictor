"use client";

import { useState } from "react";
import { useSeasonsStore } from "@/features/seasons/seasons-store";
import { trainModels } from "@/features/training/training-service";
import type { TrainModelsResponse } from "@/features/training/training-types";
import { ModelMetricCard } from "@/features/training/components/ModelMetricCard";
import { Button } from "@/shared/components/Button";
import { Card } from "@/shared/components/Card";
import { ApiError } from "@/shared/lib/api-error";

export function TrainModelsPanel() {
  const selectedYear = useSeasonsStore((state) => state.selectedYear);
  const [status, setStatus] = useState<"idle" | "running" | "success" | "error">("idle");
  const [result, setResult] = useState<TrainModelsResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleTrain() {
    setStatus("running");
    setError(null);
    try {
      const response = await trainModels(selectedYear);
      setResult(response);
      setStatus("success");
    } catch (err) {
      setError(err instanceof ApiError ? err.userMessage : "Training failed unexpectedly.");
      setStatus("error");
    }
  }

  return (
    <Card title="3. Train models">
      <p className="mb-3 text-sm text-(--color-muted)">
        Trains the podium and points-finish classifiers on {selectedYear} data, holding out the most recent
        race for the holdout view.
      </p>
      <Button onClick={handleTrain} disabled={status === "running"}>
        {status === "running" ? "Training…" : `Train on ${selectedYear}`}
      </Button>

      {status === "error" && error && <p className="mt-3 text-sm text-(--color-error-text)">{error}</p>}

      {status === "success" && result && (
        <div className="mt-4 space-y-3">
          <p className="text-sm text-(--color-muted)">
            Trained on {result.racesTrainedOn} races ({result.trainingRows} rows). Holdout race:{" "}
            <span className="font-medium">{result.holdoutRaceName}</span>.
          </p>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <ModelMetricCard label="Podium" result={result.podium} />
            <ModelMetricCard label="Points finish" result={result.pointsFinish} />
          </div>
          <p className="rounded-(--radius) bg-(--color-surface-hover) p-3 text-xs text-(--color-muted)">
            {result.metricGuidance}
          </p>
        </div>
      )}
    </Card>
  );
}
