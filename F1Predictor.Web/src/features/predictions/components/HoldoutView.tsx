"use client";

import { useEffect, useState } from "react";
import { getHoldoutPredictions } from "@/features/predictions/predictions-service";
import type { HoldoutPredictionsResponse } from "@/features/predictions/predictions-types";
import { PredictionsTable } from "@/features/predictions/components/PredictionsTable";
import { Card } from "@/shared/components/Card";
import { ErrorBanner } from "@/shared/components/ErrorBanner";
import { TableSkeleton } from "@/shared/components/Skeleton";
import { ApiError } from "@/shared/lib/api-error";
import { getErrorDisplay } from "@/shared/lib/error-display";

interface HoldoutViewProps {
  year: number;
}

export function HoldoutView({ year }: HoldoutViewProps) {
  const [data, setData] = useState<HoldoutPredictionsResponse | null>(null);
  const [error, setError] = useState<ApiError | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset before refetching on year change
    setLoading(true);
    setError(null);

    getHoldoutPredictions(year)
      .then((response) => {
        if (!cancelled) setData(response);
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof ApiError ? err : new ApiError(0, "Unknown", "Something went wrong."));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [year]);

  if (loading) {
    return (
      <Card>
        <TableSkeleton rows={10} />
      </Card>
    );
  }

  if (error?.code === "Prediction.ModelsNotTrained") {
    return (
      <ErrorBanner
        title="Models not trained yet"
        message={`Train the models for ${year} before viewing the holdout race.`}
        action={{ label: "Go train models", href: "/dashboard" }}
      />
    );
  }

  if (error?.code === "Prediction.RaceNotFound" || error?.code === "Holdout.NoRaces") {
    return <ErrorBanner title="No holdout race" message="No race data exists for this season yet." />;
  }

  if (error) {
    const { title, message } = getErrorDisplay(error);
    return <ErrorBanner title={title} message={message} />;
  }

  if (!data) {
    return null;
  }

  return (
    <Card title={`${data.meetingName} — ${data.circuitShortName} (${data.year} holdout)`}>
      <PredictionsTable drivers={data.drivers} />
    </Card>
  );
}
