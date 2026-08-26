import type { Metadata } from "next";
import Link from "next/link";
import { buttonClasses } from "@/shared/components/Button";

export const metadata: Metadata = {
  title: "Page not found",
  robots: { index: false, follow: false },
};

export default function NotFound() {
  return (
    <div className="flex flex-1 items-center justify-center px-6 py-16">
      <div className="max-w-md text-center">
        <p className="text-2xl font-bold tracking-wide">GridMind</p>
        <p className="mt-6 font-mono text-sm font-semibold uppercase tracking-wider text-(--color-muted)">
          404
        </p>
        <h1 className="mt-2 text-xl font-semibold">Page not found</h1>
        <p className="mt-2 text-sm text-(--color-muted)">
          We couldn&apos;t find the page you were looking for. It may have moved, or the link may be out of
          date.
        </p>
        <Link href="/" className={buttonClasses("primary", "mt-6")}>
          Back to home
        </Link>
      </div>
    </div>
  );
}
