import Link from "next/link";
import { TabNav } from "@/shared/components/TabNav";

export function Header() {
  return (
    <header className="border-b border-(--color-border) px-6 py-4">
      <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3">
        <Link href="/dashboard" className="text-lg font-semibold">
          GridMind
        </Link>
        <TabNav />
      </div>
    </header>
  );
}
