import type { ModelTrainingResult } from "@/features/training/training-types";

interface ModelMetricCardProps {
  label: string;
  result: ModelTrainingResult;
}

export function ModelMetricCard({ label, result }: ModelMetricCardProps) {
  return (
    <div className="rounded-md border border-(--color-border) p-3">
      <p className="text-sm font-semibold">{label}</p>
      <dl className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1 text-sm">
        <dt className="text-(--color-muted)">AUC</dt>
        <dd className="text-right font-mono">{result.areaUnderRocCurve.toFixed(3)}</dd>
        <dt className="text-(--color-muted)">F1</dt>
        <dd className="text-right font-mono">{result.f1Score.toFixed(3)}</dd>
        <dt className="text-(--color-muted)">Training rows</dt>
        <dd className="text-right font-mono">{result.trainingRowCount}</dd>
      </dl>
    </div>
  );
}
