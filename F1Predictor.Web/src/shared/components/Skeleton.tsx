interface SkeletonProps {
  className?: string;
}

/** A pulsing placeholder block. `prefers-reduced-motion` freezes the pulse globally (globals.css). */
export function Skeleton({ className }: SkeletonProps) {
  return <div aria-hidden="true" className={`animate-pulse rounded-(--radius) bg-(--color-surface-hover) ${className ?? ""}`} />;
}

interface TableSkeletonProps {
  rows?: number;
}

/** Matches the shape of the table it's standing in for, per the "no generic spinner" rule. */
export function TableSkeleton({ rows = 6 }: TableSkeletonProps) {
  return (
    <div className="space-y-2" role="status" aria-label="Loading data">
      {Array.from({ length: rows }, (_, index) => (
        <Skeleton key={index} className="h-8 w-full" />
      ))}
    </div>
  );
}

interface CardSkeletonProps {
  count?: number;
}

/** Matches the shape of a stacked-card list (e.g. ScenariosView) rather than a table. */
export function CardSkeleton({ count = 3 }: CardSkeletonProps) {
  return (
    <div className="space-y-3" role="status" aria-label="Loading data">
      {Array.from({ length: count }, (_, index) => (
        <Skeleton key={index} className="h-32 w-full" />
      ))}
    </div>
  );
}
