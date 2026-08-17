"use client";

import { useEffect, useState } from "react";
import { getRacePredictions } from "@/features/predictions/predictions-service";
import type { RacePredictionsResponse } from "@/features/predictions/predictions-types";
import { PredictionsTable } from "@/features/predictions/components/PredictionsTable";
import { Card } from "@/shared/components/Card";
import { ErrorBanner } from "@/shared/components/ErrorBanner";
import { ApiError } from "@/shared/lib/api-error";

interface RacePredictionsViewProps {
  sessionKey: number;
}

export function RacePredictionsView({ sessionKey }: RacePredictionsViewProps) {
  const [data, setData] = useState<RacePredictionsResponse | null>(null);
  const [error, setError] = useState<ApiError | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset before refetching on sessionKey change
    setLoading(true);
    setError(null);

    getRacePredictions(sessionKey)
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
  }, [sessionKey]);

  if (loading) {
    return <p className="text-sm text-(--color-muted)">Loading predictions…</p>;
  }

  if (error?.code === "Prediction.ModelsNotTrained") {
    return (
      <ErrorBanner
        title="Models not trained yet"
        message="Train the models for this season before viewing predictions."
        action={{ label: "Go train models", href: "/dashboard" }}
      />
    );
  }

  if (error?.code === "Prediction.RaceNotFound") {
    return <ErrorBanner title="Race not found" message="No feature data exists for this race. Check it was ingested." />;
  }

  if (error) {
    return <ErrorBanner title="Something went wrong" message={error.userMessage} />;
  }

  if (!data) {
    return null;
  }

  return (
    <Card title={data.meetingName}>
      <PredictionsTable drivers={data.drivers} />
    </Card>
  );
}
