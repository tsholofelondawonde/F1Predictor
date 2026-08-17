import Link from "next/link";
import { buttonClasses } from "@/shared/components/Button";
import { LivePreviewCard } from "@/features/landing/components/LivePreviewCard";

export function Hero() {
  return (
    <section className="relative overflow-hidden bg-(--color-landing-bg)">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-40 left-1/2 h-[36rem] w-[36rem] -translate-x-1/2 rounded-full blur-3xl"
        style={{ backgroundColor: "var(--color-accent-glow)" }}
      />

      <div className="relative mx-auto grid max-w-6xl gap-12 px-6 py-20 md:grid-cols-2 md:items-center md:py-28">
        <div>
          <h1 className="font-display text-5xl leading-[0.95] tracking-wide sm:text-6xl">
            Every grid, every gap to pole, one probability.
          </h1>
          <p className="mt-6 max-w-md text-base text-(--color-muted)">
            GridMind turns live OpenF1 timing data into podium and points-finish probabilities,
            championship title odds, and what-if title scenarios — before and after qualifying.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link href="/dashboard" className={buttonClasses("primary")}>
              Open the dashboard
            </Link>
            <a href="#how-it-works" className={buttonClasses("secondary")}>
              See how it works
            </a>
          </div>
        </div>

        <LivePreviewCard />
      </div>
    </section>
  );
}
