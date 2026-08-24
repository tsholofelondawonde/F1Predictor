import Link from "next/link";
import { buttonClasses } from "@/shared/components/Button";

export function MarketingNav() {
  return (
    <header className="sticky top-0 z-10 border-b border-(--color-landing-border) bg-(--color-landing-bg)/80 backdrop-blur-sm">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-4">
        <Link href="/" className="font-display text-2xl tracking-wide">
          GridMind
        </Link>

        <nav className="flex items-center gap-6 text-sm text-(--color-muted)" aria-label="Page sections">
          <a href="#features" className="transition-colors hover:text-(--color-foreground)">
            Features
          </a>
          <a href="#how-it-works" className="transition-colors hover:text-(--color-foreground)">
            How it works
          </a>
        </nav>

        <Link href="/dashboard" className={buttonClasses("primary")}>
          Open the dashboard
        </Link>
      </div>
    </header>
  );
}
