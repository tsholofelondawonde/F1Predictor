import { Card } from "@/shared/components/Card";

const PROOF_POINTS = [
  {
    title: "AUC & F1, not accuracy",
    body: "Podium finishes are roughly 15% of the field, so a plain accuracy score would look artificially high. We report the metrics that actually measure a classifier on an imbalanced target.",
  },
  {
    title: "10,000-run Monte Carlo",
    body: "Every championship forecast simulates the rest of the season ten thousand times from a Plackett-Luce driver strength model.",
  },
  {
    title: "Every model ships its own caveat",
    body: "Training results carry a plain-language note about sample size, so a one-season model doesn't get oversold as more than it is.",
  },
  {
    title: "OpenF1, live",
    body: "Free, public F1 timing data from 2023 onward — no paywalled feed.",
  },
] as const;

export function ProofStrip() {
  return (
    <section className="border-y border-(--color-landing-border) bg-(--color-landing-surface)">
      <div className="mx-auto max-w-6xl px-6 py-16">
        <h2 className="font-display text-3xl tracking-wide">Honest numbers, not vanity metrics</h2>
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {PROOF_POINTS.map((point) => (
            <Card key={point.title} title={point.title}>
              <p className="text-sm text-(--color-muted)">{point.body}</p>
            </Card>
          ))}
        </div>
      </div>
    </section>
  );
}
