"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError } from "@/shared/lib/api-error";

export const DEFAULT_REFRESH_MS = 60_000;

interface LiveData<T> {
  data: T | null;
  error: ApiError | null;
  /** True only on the very first load, so a refresh never blanks the page. */
  loading: boolean;
  /** True while a background refresh is in flight. */
  refreshing: boolean;
  lastUpdated: Date | null;
  refresh: () => void;
}

/**
 * Fetches once, then again on an interval, keeping the previous data visible throughout.
 *
 * This is what "live" means here: the backend derives standings and odds from the database and
 * caches the simulation, so re-reading on a timer is cheap and keeps the page current without a
 * push channel or a background service. A tab that is not visible stops polling — there is no
 * point spending requests on a page nobody is looking at.
 */
export function useLiveData<T>(
  fetcher: () => Promise<T>,
  deps: readonly unknown[],
  intervalMs: number = DEFAULT_REFRESH_MS,
): LiveData<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<ApiError | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  // Held in a ref so changing the fetcher identity on every render does not restart the timer.
  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;

  const [tick, setTick] = useState(0);
  const refresh = useCallback(() => setTick((value) => value + 1), []);

  useEffect(() => {
    let cancelled = false;
    let first = true;

    async function load() {
      if (!first) setRefreshing(true);

      try {
        const response = await fetcherRef.current();
        if (cancelled) return;

        setData(response);
        setError(null);
        setLastUpdated(new Date());
      } catch (err) {
        if (cancelled) return;

        setError(err instanceof ApiError ? err : new ApiError(0, "Unknown", "Something went wrong."));
      } finally {
        if (!cancelled) {
          setLoading(false);
          setRefreshing(false);
          first = false;
        }
      }
    }

    void load();

    const timer = setInterval(() => {
      if (document.visibilityState === "visible") void load();
    }, intervalMs);

    return () => {
      cancelled = true;
      clearInterval(timer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- deps are the caller's fetch inputs
  }, [intervalMs, tick, ...deps]);

  return { data, error, loading, refreshing, lastUpdated, refresh };
}
