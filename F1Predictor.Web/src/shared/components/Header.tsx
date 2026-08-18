import Link from "next/link";
import { TabNav } from "@/shared/components/TabNav";

export function Header() {
  return (
    <header className="border-b border-(--color-border) px-6 py-4">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3">
        <Link
          href="/dashboard"
          className="font-sans text-lg font-black uppercase tracking-tight focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent)"
        >
          GridMind
        </Link>
        <TabNav />
      </div>
    </header>
  );
}
