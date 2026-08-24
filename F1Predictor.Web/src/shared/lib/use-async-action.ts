"use client";

import { useCallback, useState } from "react";
import { ApiError } from "@/shared/lib/api-error";
import { getErrorDisplay, type ErrorDisplay } from "@/shared/lib/error-display";

type Status = "idle" | "running" | "success" | "error";

interface AsyncAction<TResult, TArgs extends unknown[]> {
  status: Status;
  result: TResult | null;
  error: ErrorDisplay | null;
  run: (...args: TArgs) => Promise<void>;
}

/**
 * One idle/running/success/error state machine for a triggered async action, so callers
 * (ingest, rebuild, train) don't each hand-roll the same three useState calls.
 */
export function useAsyncAction<TResult, TArgs extends unknown[] = []>(
  action: (...args: TArgs) => Promise<TResult>,
): AsyncAction<TResult, TArgs> {
  const [status, setStatus] = useState<Status>("idle");
  const [result, setResult] = useState<TResult | null>(null);
  const [error, setError] = useState<ErrorDisplay | null>(null);

  const run = useCallback(
    async (...args: TArgs) => {
      if (status === "running") return;

      setStatus("running");
      setError(null);

      try {
        const response = await action(...args);
        setResult(response);
        setStatus("success");
      } catch (err) {
        setError(err instanceof ApiError ? getErrorDisplay(err) : { title: "Something went wrong", message: "Something went wrong unexpectedly." });
        setStatus("error");
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps -- action identity is the caller's concern, not a dep
    [status],
  );

  return { status, result, error, run };
}
