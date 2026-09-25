"use client";

import { useCallback, useState } from "react";
import { ingestSeason, rebuildFeatures } from "@/features/seasons/seasons-service";
import { trainModels } from "@/features/training/training-service";
import { ApiError } from "@/shared/lib/api-error";
import { getErrorDisplay, type ErrorDisplay } from "@/shared/lib/error-display";

type Status = "idle" | "running" | "success" | "error";
type Step = "ingest" | "rebuild" | "train";

const STEP_LABELS: Record<Step, string> = {
  ingest: "Ingesting latest results…",
  rebuild: "Rebuilding features…",
  train: "Training models…",
};

interface IngestAndRetrain {
  status: Status;
  stepLabel: string | null;
  error: ErrorDisplay | null;
  run: (year: number) => Promise<void>;
}

/**
 * Runs the Ingest -> Rebuild -> Train pipeline in sequence for one year, so a next-race-page
 * prompt can trigger the same maintenance steps as the dashboard's three admin panels in one
 * click, with a step label the caller can show while it's running.
 */
export function useIngestAndRetrain(): IngestAndRetrain {
  const [status, setStatus] = useState<Status>("idle");
  const [step, setStep] = useState<Step | null>(null);
  const [error, setError] = useState<ErrorDisplay | null>(null);

  const run = useCallback(
    async (year: number) => {
      if (status === "running") return;

      setStatus("running");
      setError(null);

      try {
        setStep("ingest");
        await ingestSeason(year);
        setStep("rebuild");
        await rebuildFeatures();
        setStep("train");
        await trainModels(year);
        setStatus("success");
      } catch (err) {
        setError(err instanceof ApiError ? getErrorDisplay(err) : { title: "Something went wrong", message: "Something went wrong unexpectedly." });
        setStatus("error");
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps -- mirrors use-async-action's own justification: identity of the service calls isn't a dep
    [status],
  );

  return { status, stepLabel: step ? STEP_LABELS[step] : null, error, run };
}
