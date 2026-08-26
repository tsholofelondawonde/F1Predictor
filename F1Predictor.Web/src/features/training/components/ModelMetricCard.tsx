import type { ModelTrainingResult } from "@/features/training/training-types";

interface ModelMetricCardProps {
  label: string;
  result: ModelTrainingResult;
}

export function ModelMetricCard({ label, result }: ModelMetricCardProps) {
  return (
    <div className="rounded-(--radius) border border-(--color-border) p-3">
      <p className="font-mono text-xs font-semibold uppercase tracking-wider">{label}</p>
      <dl className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1 text-sm">
        <dt className="text-(--color-muted)">AUC</dt>
        <dd className="text-right font-mono tabular-nums">{result.areaUnderRocCurve.toFixed(3)}</dd>
        <dt className="text-(--color-muted)">F1</dt>
        <dd className="text-right font-mono tabular-nums">{result.f1Score.toFixed(3)}</dd>
        <dt className="text-(--color-muted)">Training rows</dt>
        <dd className="text-right font-mono tabular-nums">{result.trainingRowCount}</dd>
      </dl>
    </div>
  );
}
