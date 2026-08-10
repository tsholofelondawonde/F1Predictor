"use client";

import { useSeasonsStore } from "@/features/seasons/seasons-store";
import { ingestSeason } from "@/features/seasons/seasons-service";
import { IngestOutcomeLabel } from "@/features/seasons/seasons-types";
import { Button } from "@/shared/components/Button";
import { Card } from "@/shared/components/Card";
import { ApiError } from "@/shared/lib/api-error";

export function IngestSeasonPanel() {
  const selectedYear = useSeasonsStore((state) => state.selectedYear);
  const ingestStatus = useSeasonsStore((state) => state.ingestStatus);
  const lastIngestResponse = useSeasonsStore((state) => state.lastIngestResponse);
  const lastIngestError = useSeasonsStore((state) => state.lastIngestError);
  const setIngestRunning = useSeasonsStore((state) => state.setIngestRunning);
  const setIngestSuccess = useSeasonsStore((state) => state.setIngestSuccess);
  const setIngestError = useSeasonsStore((state) => state.setIngestError);

  const isRunning = ingestStatus === "running";

  async function handleIngest() {
    setIngestRunning();
    try {
      const response = await ingestSeason(selectedYear);
      setIngestSuccess(response);
    } catch (error) {
      setIngestError(error instanceof ApiError ? error.userMessage : "Ingest failed unexpectedly.");
    }
  }

  return (
    <Card title="1. Ingest season">
      <p className="mb-3 text-sm text-(--color-muted)">
        Pulls meetings, sessions, results, grid, pit stops, and weather from OpenF1 for the selected season.
        Safe to re-run — already-ingested race weekends are skipped. A full season can take several minutes
        (OpenF1 is called politely, with a delay between requests), so this won&apos;t return instantly.
      </p>
      <Button onClick={handleIngest} disabled={isRunning}>
        {isRunning ? "Ingesting… this can take several minutes" : `Ingest ${selectedYear}`}
      </Button>

      {ingestStatus === "error" && lastIngestError && (
        <p className="mt-3 text-sm text-(--color-error-text)">{lastIngestError}</p>
      )}

      {ingestStatus === "success" && lastIngestResponse && (
        <div className="mt-4 text-sm">
          <p className="mb-2 text-(--color-muted)">
            {lastIngestResponse.meetingsIngested} of {lastIngestResponse.meetingsFound} meetings newly ingested.
          </p>
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-(--color-border) text-(--color-muted)">
                <th className="py-1 pr-2 font-medium">Meeting</th>
                <th className="py-1 font-medium">Outcome</th>
              </tr>
            </thead>
            <tbody>
              {lastIngestResponse.meetings.map((meeting) => (
                <tr key={meeting.meetingName} className="border-b border-(--color-border) last:border-0">
                  <td className="py-1 pr-2">{meeting.meetingName}</td>
                  <td className="py-1">{IngestOutcomeLabel[meeting.outcome]}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}
