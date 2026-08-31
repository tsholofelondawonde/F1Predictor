import Link from "next/link";
import { TabNav } from "@/shared/components/TabNav";
import { Logo } from "@/shared/components/Logo";

export function Header() {
  return (
    <header className="border-b border-(--color-border) px-4 py-4 sm:px-6 lg:px-8">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3">
        <Link
          href="/dashboard"
          className="inline-flex items-center gap-2 font-sans text-lg font-black uppercase tracking-tight focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent)"
        >
          <Logo className="h-6 w-6 shrink-0" />
          GridMind
        </Link>
        <TabNav />
      </div>
    </header>
  );
}
