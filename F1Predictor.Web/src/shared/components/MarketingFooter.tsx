import Link from "next/link";

export function MarketingFooter() {
  return (
    <footer className="border-t border-(--color-landing-border) bg-(--color-landing-bg)">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-8 text-sm text-(--color-muted)">
        <span className="font-display text-xl tracking-wide text-(--color-foreground)">GridMind</span>
        <p>Built on live OpenF1 data · ML.NET SDCA classifiers · Monte Carlo championship simulation</p>
        <Link href="/dashboard" className="font-medium text-(--color-accent) hover:underline">
          Open the dashboard
        </Link>
      </div>
    </footer>
  );
}
