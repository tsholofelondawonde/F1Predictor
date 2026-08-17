"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const TABS = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/next-race", label: "Next Race" },
  { href: "/championship", label: "Championship" },
  { href: "/scenarios", label: "Scenarios" },
  { href: "/holdout", label: "Holdout" },
] as const;

function isActive(pathname: string, href: string): boolean {
  return pathname.startsWith(href);
}

export function TabNav() {
  const pathname = usePathname();

  return (
    <nav className="flex gap-1 text-sm" aria-label="Sections">
      {TABS.map((tab) => {
        const active = isActive(pathname, tab.href);

        return (
          <Link
            key={tab.href}
            href={tab.href}
            aria-current={active ? "page" : undefined}
            className={`rounded-md px-3 py-1.5 transition-colors ${
              active
                ? "bg-(--color-surface) font-medium text-(--color-accent)"
                : "text-(--color-muted) hover:bg-(--color-surface-hover) hover:text-(--color-foreground)"
            }`}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}
