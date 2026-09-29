import type { ModelTrainingResult, TargetEvaluation } from "@/features/training/training-types";

interface ModelMetricCardProps {
  label: string;
  result: ModelTrainingResult;
  evaluation: TargetEvaluation;
}

const formatMetric = (value: number | null) => (value === null ? "n/a" : value.toFixed(3));

export function ModelMetricCard({ label, result, evaluation }: ModelMetricCardProps) {
  const lift = evaluation.baselineAuc === null ? null : result.areaUnderRocCurve - evaluation.baselineAuc;

  return (
    <div className="rounded-(--radius) border border-(--color-border) p-3">
      <p className="font-mono text-xs font-semibold uppercase tracking-wider">{label}</p>
      <p className="mt-1 text-xs text-(--color-muted)">{result.trainerName}</p>
      <dl className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1 text-sm">
        <dt className="text-(--color-muted)">Validation AUC</dt>
        <dd className="text-right font-mono tabular-nums">{result.areaUnderRocCurve.toFixed(3)}</dd>
        <dt className="text-(--color-muted)">Grid-order AUC</dt>
        <dd className="text-right font-mono tabular-nums">{formatMetric(evaluation.baselineAuc)}</dd>
        <dt className="text-(--color-muted)">Beats grid by</dt>
        <dd
          className={`text-right font-mono tabular-nums ${lift !== null && lift <= 0 ? "text-(--color-error-text)" : ""}`}
        >
          {lift === null ? "n/a" : `${lift > 0 ? "+" : ""}${lift.toFixed(3)}`}
        </dd>
        <dt className="text-(--color-muted)">Log loss (bits)</dt>
        <dd className="text-right font-mono tabular-nums">{result.logLoss.toFixed(3)}</dd>
        <dt className="text-(--color-muted)">F1</dt>
        <dd className="text-right font-mono tabular-nums">{result.f1Score.toFixed(3)}</dd>
        <dt className="text-(--color-muted)">Holdout AUC</dt>
        <dd className="text-right font-mono tabular-nums">{formatMetric(evaluation.holdoutAuc)}</dd>
        <dt className="text-(--color-muted)">Train / validation rows</dt>
        <dd className="text-right font-mono tabular-nums">
          {result.trainingRowCount} / {result.validationRowCount}
        </dd>
      </dl>
    </div>
  );
}
