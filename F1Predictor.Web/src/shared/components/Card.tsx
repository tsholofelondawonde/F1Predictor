import type { ReactNode } from "react";

interface CardProps {
  title?: string;
  children: ReactNode;
  className?: string;
}

export function Card({ title, children, className }: CardProps) {
  return (
    <section className={`rounded-lg border border-(--color-border) bg-(--color-surface) p-5 ${className ?? ""}`}>
      {title && <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-(--color-muted)">{title}</h2>}
      {children}
    </section>
  );
}
