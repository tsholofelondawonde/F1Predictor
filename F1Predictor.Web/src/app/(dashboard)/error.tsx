"use client";

import { useEffect } from "react";
import { Button } from "@/shared/components/Button";
import { ErrorBanner } from "@/shared/components/ErrorBanner";

interface DashboardErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function DashboardError({ error, reset }: DashboardErrorProps) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="space-y-4">
      <ErrorBanner
        title="Something went wrong"
        message="This page hit an unexpected error. Try again, or use the tabs above to go elsewhere."
      />
      <Button onClick={reset} variant="secondary">
        Try again
      </Button>
    </div>
  );
}
