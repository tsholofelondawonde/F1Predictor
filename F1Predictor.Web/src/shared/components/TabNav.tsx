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
    <nav className="flex font-mono text-xs uppercase tracking-wider" aria-label="Sections">
      {TABS.map((tab) => {
        const active = isActive(pathname, tab.href);

        return (
          <Link
            key={tab.href}
            href={tab.href}
            aria-current={active ? "page" : undefined}
            className={`border-b-2 px-3 py-2 transition-colors focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-(--color-accent) ${
              active
                ? "border-(--color-accent) text-(--color-accent)"
                : "border-transparent text-(--color-muted) hover:border-(--color-border) hover:text-(--color-foreground)"
            }`}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}
