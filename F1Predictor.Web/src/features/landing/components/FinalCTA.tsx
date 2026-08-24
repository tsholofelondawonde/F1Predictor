import Link from "next/link";
import { buttonClasses } from "@/shared/components/Button";
import { RevealOnScroll } from "@/shared/components/RevealOnScroll";

export function FinalCTA() {
  return (
    <section className="relative overflow-hidden border-t border-(--color-landing-border) bg-(--color-landing-surface)">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -bottom-40 left-1/2 h-[36rem] w-[36rem] -translate-x-1/2 rounded-full blur-3xl"
        style={{ backgroundColor: "var(--color-accent-glow)" }}
      />

      <RevealOnScroll className="relative mx-auto flex max-w-4xl flex-col items-center gap-6 px-6 py-20 text-center">
        <h2 className="font-display text-4xl tracking-wide sm:text-5xl">
          See this weekend&apos;s grid the way the model sees it.
        </h2>
        <Link href="/dashboard" className={buttonClasses("primary", "px-8 py-3 text-base")}>
          Open the dashboard
        </Link>
      </RevealOnScroll>
    </section>
  );
}
