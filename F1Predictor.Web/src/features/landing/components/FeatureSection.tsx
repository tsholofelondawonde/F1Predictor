import type { ReactNode } from "react";

interface FeatureSectionProps {
  icon: ReactNode;
  title: string;
  description: string;
  reversed?: boolean;
}

export function FeatureSection({ icon, title, description, reversed = false }: FeatureSectionProps) {
  return (
    <div className={`flex flex-col items-start gap-4 ${reversed ? "md:items-end md:text-right" : ""}`}>
      <span className="rounded-lg bg-(--color-accent)/10 p-3 text-(--color-accent)">{icon}</span>
      <h3 className="text-xl font-semibold">{title}</h3>
      <p className="max-w-md text-sm text-(--color-muted)">{description}</p>
    </div>
  );
}
