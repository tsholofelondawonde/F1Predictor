"use client";

import { useEffect, useState } from "react";

interface CountdownProps {
  to: string | Date;
  className?: string;
}

export function formatCountdown(ms: number): string {
  if (ms <= 0) return "under way";

  const hours = Math.floor(ms / 3_600_000);
  const days = Math.floor(hours / 24);

  if (days >= 1) return `in ${days} day${days === 1 ? "" : "s"}`;
  if (hours >= 1) return `in ${hours}h ${Math.floor((ms % 3_600_000) / 60_000)}m`;

  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;

  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

/**
 * A live-ticking countdown to a fixed instant. Ticks on its own 1s timer, independent of
 * whatever polling loop supplied `to`, so the clock stays smooth between data refreshes.
 *
 * No `aria-live` here on purpose — it typically sits inline in a sentence, and announcing a
 * per-second update would spam screen readers. Live-region duties belong to the surrounding
 * page (see LiveFooter), not this element.
 */
export function Countdown({ to, className }: CountdownProps) {
  const target = typeof to === "string" ? new Date(to) : to;
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  return (
    <span className={className ?? "font-mono text-xs uppercase tracking-wider"}>
      {formatCountdown(target.getTime() - now)}
    </span>
  );
}
