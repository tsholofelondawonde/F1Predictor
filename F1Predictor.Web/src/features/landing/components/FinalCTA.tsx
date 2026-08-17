import Link from "next/link";
import { buttonClasses } from "@/shared/components/Button";

export function FinalCTA() {
  return (
    <section className="border-t border-(--color-landing-border) bg-(--color-landing-surface)">
      <div className="mx-auto flex max-w-4xl flex-col items-center gap-6 px-6 py-20 text-center">
        <h2 className="font-display text-4xl tracking-wide sm:text-5xl">
          See this weekend&apos;s grid the way the model sees it.
        </h2>
        <Link href="/dashboard" className={buttonClasses("primary", "px-8 py-3 text-base")}>
          Open the dashboard
        </Link>
      </div>
    </section>
  );
}
