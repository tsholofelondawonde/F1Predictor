"use client";

import { useSeasonsStore } from "@/features/seasons/seasons-store";
import { trainModels } from "@/features/training/training-service";
import { ModelMetricCard } from "@/features/training/components/ModelMetricCard";
import { Button } from "@/shared/components/Button";
import { Card } from "@/shared/components/Card";
import { StatusAnnouncer } from "@/shared/components/StatusAnnouncer";
import { useAsyncAction } from "@/shared/lib/use-async-action";

export function TrainModelsPanel() {
  const selectedYear = useSeasonsStore((state) => state.selectedYear);
  const { status, result, error, run } = useAsyncAction(trainModels);

  return (
    <Card title="3. Train models">
      <p className="mb-3 text-sm text-(--color-muted)">
        Trains the podium and points-finish classifiers on {selectedYear} data, holding out the most recent
        race for the holdout view.
      </p>
      <Button onClick={() => run(selectedYear)} disabled={status === "running"}>
        {status === "running" ? "Training…" : `Train on ${selectedYear}`}
      </Button>

      <StatusAnnouncer
        message={
          status === "success" && result
            ? `Training complete. Holdout race: ${result.holdoutRaceName}.`
            : status === "error" && error
              ? `Training failed: ${error.message}`
              : null
        }
      />

      {status === "error" && error && <p className="mt-3 text-sm text-(--color-error-text)">{error.message}</p>}

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
