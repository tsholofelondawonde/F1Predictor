"use client";

import { useEffect } from "react";
import { Button } from "@/shared/components/Button";
import { ErrorBanner } from "@/shared/components/ErrorBanner";

interface RacePredictionsErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function RacePredictionsError({ error, reset }: RacePredictionsErrorProps) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="space-y-4">
      <ErrorBanner
        title="Couldn't load this race"
        message="Something went wrong fetching this race's predictions. Try again, or check it was ingested."
        action={{ label: "Go to dashboard", href: "/dashboard" }}
      />
      <Button onClick={reset} variant="secondary">
        Try again
      </Button>
    </div>
  );
}
