import type { Metadata } from "next";
import Link from "next/link";
import { buttonClasses } from "@/shared/components/Button";
import { RevealOnScroll, RevealListItemOnScroll } from "@/shared/components/RevealOnScroll";

const title = "Roadmap";
const description =
  "What GridMind is building next, the scope cuts it's shipping around today, and the AI analysis layer already designed but not yet built.";

export const metadata: Metadata = {
  title,
  description,
  openGraph: { title, description },
  twitter: { title, description },
  alternates: { canonical: "/roadmap" },
};

const NEXT_STEPS = [
  {
    title: "More training data",
    body: "Ingest 2–3 seasons instead of one, for a less anemic training set.",
  },
  {
    title: "Tyre & strategy features",
    body: "Add the stints endpoint — tyre compound and strategy features.",
  },
  {
    title: "AutoML upgrade path",
    body: "Try ML.NET's AutoML API once the SDCA baseline is trusted.",
  },
  {
    title: "Background ingestion",
    body: "Move ingestion onto a background queue so the endpoint returns 202 immediately.",
  },
  {
    title: "Retire the legacy import",
    body: "Remove the one-off SQLite importer used to migrate the original console prototype.",
  },
] as const;

const SCOPE_CUTS = [
  {
    title: "Single season of data",
    body: "~480 driver-race rows — enough to demonstrate the pipeline end to end, not enough for the predictions to be authoritative.",
  },
  {
    title: "No tyre or strategy features",
    body: "The stints endpoint isn't ingested yet.",
  },
  {
    title: "Qualifying gap sentinel",
    body: "A missing qualifying lap gets a 99 sentinel value rather than proper imputation.",
  },
  {
    title: "Synchronous ingestion",
    body: "Ingestion runs inside the request. A background queue is a later concern.",
  },
  {
    title: "Legacy SQLite import",
    body: "Exists only to migrate the original prototype's data — development-only, slated for removal.",
  },
] as const;

export default function RoadmapPage() {
  return (
    <>
      <section className="mx-auto max-w-6xl px-6 py-20">
        <RevealOnScroll>
          <h1 className="font-display text-4xl tracking-wide sm:text-5xl">Roadmap</h1>
          <p className="mt-4 max-w-2xl text-(--color-muted)">
            GridMind is a working prediction pipeline, not a finished product. Here&apos;s
            what&apos;s shipping next, the boundaries it&apos;s deliberately shipping around
            today, and an analysis layer that&apos;s already designed but not yet built.
          </p>
        </RevealOnScroll>
      </section>

      <section className="mx-auto max-w-6xl px-6 pb-20">
        <h2 className="font-display text-3xl tracking-wide">What&apos;s next</h2>
        <ol className="mt-10 grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
          {NEXT_STEPS.map((step, index) => (
            <RevealListItemOnScroll
              key={step.title}
              delay={index * 0.06}
              className="flex flex-col gap-3"
            >
              <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-(--color-accent) font-mono text-xs font-semibold text-(--color-on-accent)">
                {index + 1}
              </span>
              <h3 className="text-base font-semibold">{step.title}</h3>
              <p className="text-sm text-(--color-muted)">{step.body}</p>
            </RevealListItemOnScroll>
          ))}
        </ol>
      </section>

      <section className="border-t border-(--color-landing-border) bg-(--color-landing-surface)">
        <div className="mx-auto max-w-6xl px-6 py-20">
          <h2 className="font-display text-3xl tracking-wide">Known scope cuts</h2>
          <p className="mt-3 max-w-2xl text-sm text-(--color-muted)">
            Deliberate boundaries for a first release, not oversights.
          </p>
          <ul className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {SCOPE_CUTS.map((cut, index) => (
              <RevealListItemOnScroll
                key={cut.title}
                delay={index * 0.06}
                className="rounded-(--radius) border border-(--color-landing-border) p-5"
              >
                <h3 className="text-base font-semibold">{cut.title}</h3>
                <p className="mt-2 text-sm text-(--color-muted)">{cut.body}</p>
              </RevealListItemOnScroll>
            ))}
          </ul>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-6 py-20">
        <RevealOnScroll>
          <h2 className="font-display text-3xl tracking-wide">Planned: an AI analysis layer</h2>
          <p className="mt-3 max-w-3xl text-sm text-(--color-muted)">
            Designed, not yet built. Two approved specs add interpretation beside the
            prediction pipeline, never inside it — the classifiers and the Monte Carlo
            simulator stay the only source of numbers.
          </p>
        </RevealOnScroll>

        <div className="mt-10 grid gap-8 md:grid-cols-2">
          <RevealOnScroll delay={0.06} className="flex flex-col gap-3">
            <h3 className="text-lg font-semibold">Explanations & analyst chat</h3>
            <p className="text-sm text-(--color-muted)">
              Coefficient-grounded, per-feature explanations for a prediction; a generated
              race-preview narrative; and a tool-calling analyst chat that answers questions by
              calling GridMind&apos;s own use cases — running on a local Ollama model.
            </p>
          </RevealOnScroll>
          <RevealOnScroll delay={0.12} className="flex flex-col gap-3">
            <h3 className="text-lg font-semibold">Hosted provider & semantic search</h3>
            <p className="text-sm text-(--color-muted)">
              A hosted OpenAI provider for chat and embeddings in production, explanations
              extended to races that have already run, a background job that keeps previews
              fresh, and semantic search over past races via embedded fact sheets in Postgres.
            </p>
          </RevealOnScroll>
        </div>
      </section>

      <section className="relative overflow-hidden border-t border-(--color-landing-border) bg-(--color-landing-surface)">
        <RevealOnScroll className="relative mx-auto flex max-w-4xl flex-col items-center gap-6 px-6 py-20 text-center">
          <h2 className="font-display text-3xl tracking-wide sm:text-4xl">
            Follow along in the dashboard.
          </h2>
          <Link href="/dashboard" className={buttonClasses("primary", "px-8 py-3 text-base")}>
            Open the dashboard
          </Link>
        </RevealOnScroll>
      </section>
    </>
  );
}
