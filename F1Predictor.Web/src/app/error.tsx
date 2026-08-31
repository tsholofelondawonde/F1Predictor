"use client";

import { useEffect } from "react";
import Link from "next/link";
import { buttonClasses } from "@/shared/components/Button";

interface RootErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function RootError({ error, reset }: RootErrorProps) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="flex flex-1 items-center justify-center px-6 py-16">
      <div className="max-w-md text-center">
        <p className="text-2xl font-bold tracking-wide">GridMind</p>
        <p className="mt-6 font-mono text-sm font-semibold uppercase tracking-wider text-(--color-muted)">
          Error
        </p>
        <h1 className="mt-2 text-xl font-semibold">Something went wrong</h1>
        <p className="mt-2 text-sm text-(--color-muted)">
          An unexpected error occurred. You can try again, or head back to the home page.
        </p>
        <div className="mt-6 flex items-center justify-center gap-3">
          <button type="button" onClick={reset} className={buttonClasses("secondary")}>
            Try again
          </button>
          <Link href="/" className={buttonClasses("primary")}>
            Back to home
          </Link>
        </div>
      </div>
    </div>
  );
}
