import type { ReactNode } from "react";

interface CardProps {
  title?: string;
  children: ReactNode;
  className?: string;
}

export function Card({ title, children, className }: CardProps) {
  return (
    <section className={`rounded-(--radius) border border-(--color-border) bg-(--color-surface) p-5 ${className ?? ""}`}>
      {title && (
        <h2 className="card-title mb-3 font-mono text-xs font-semibold uppercase tracking-[0.15em] text-(--color-muted)">
          {title}
        </h2>
      )}
      {children}
    </section>
  );
}
