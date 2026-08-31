import { DatabaseIcon, GaugeIcon, CpuIcon, TargetIcon } from "@/features/landing/components/icons";
import { RevealListItemOnScroll } from "@/shared/components/RevealOnScroll";

const STEPS = [
  {
    icon: DatabaseIcon,
    title: "Ingest",
    body: "Pull a season's sessions, results, and pit stops from OpenF1.",
  },
  {
    icon: GaugeIcon,
    title: "Engineer features",
    body: "Turn raw timing data into the five signals the models train on.",
  },
  {
    icon: CpuIcon,
    title: "Train",
    body: "Fit SDCA logistic regression classifiers for podium and points finishes, per season.",
  },
  {
    icon: TargetIcon,
    title: "Predict",
    body: "Preview the next Grand Prix from recent form before qualifying, then from the real grid once it runs.",
  },
] as const;

export function HowItWorks() {
  return (
    <section id="how-it-works" className="mx-auto max-w-6xl px-6 py-20">
      <h2 className="font-display text-3xl tracking-wide">How it works</h2>

      <ol className="mt-10 grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
        {STEPS.map((step, index) => {
          const Icon = step.icon;

          return (
            <RevealListItemOnScroll key={step.title} delay={index * 0.08} className="flex flex-col gap-3">
              <div className="flex items-center gap-3">
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-(--color-accent) font-mono text-xs font-semibold text-(--color-on-accent)">
                  {index + 1}
                </span>
                <Icon className="h-5 w-5 text-(--color-muted)" />
              </div>
              <h3 className="text-base font-semibold">{step.title}</h3>
              <p className="text-sm text-(--color-muted)">{step.body}</p>
            </RevealListItemOnScroll>
          );
        })}
      </ol>
    </section>
  );
}
