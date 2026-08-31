"use client";

import { useSeasonsStore } from "@/features/seasons/seasons-store";
import { ingestSeason } from "@/features/seasons/seasons-service";
import { IngestOutcomeLabel } from "@/features/seasons/seasons-types";
import { Button } from "@/shared/components/Button";
import { Card } from "@/shared/components/Card";
import { StatusAnnouncer } from "@/shared/components/StatusAnnouncer";
import { useAsyncAction } from "@/shared/lib/use-async-action";

export function IngestSeasonPanel() {
  const selectedYear = useSeasonsStore((state) => state.selectedYear);
  const { status, result, error, run } = useAsyncAction(ingestSeason);

  const isRunning = status === "running";

  return (
    <Card title="1. Ingest season">
      <p className="mb-3 text-sm text-(--color-muted)">
        Pulls meetings, sessions, results, grid, pit stops, and weather from OpenF1 for the selected season.
        Safe to re-run — already-ingested race weekends are skipped. A full season can take several minutes
        (OpenF1 is called politely, with a delay between requests), so this won&apos;t return instantly.
      </p>
      <Button onClick={() => run(selectedYear)} disabled={isRunning}>
        {isRunning ? "Ingesting… this can take several minutes" : `Ingest ${selectedYear}`}
      </Button>

      <StatusAnnouncer
        message={
          status === "success" && result
            ? `Ingest complete: ${result.meetingsIngested} of ${result.meetingsFound} meetings newly ingested.`
            : status === "error" && error
              ? `Ingest failed: ${error.message}`
              : null
        }
      />

      {status === "error" && error && <p className="mt-3 text-sm text-(--color-error-text)">{error.message}</p>}

      {status === "success" && result && (
        <div className="mt-4 text-sm">
          <p className="mb-2 text-(--color-muted)">
            {result.meetingsIngested} of {result.meetingsFound} meetings newly ingested.
          </p>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[480px] text-left text-sm">
              <caption className="sr-only">Meetings ingested this run and their outcome</caption>
              <thead>
                <tr className="border-b border-(--color-border) text-(--color-muted)">
                  <th scope="col" className="py-1 pr-2 font-medium">Meeting</th>
                  <th scope="col" className="py-1 font-medium">Outcome</th>
                </tr>
              </thead>
              <tbody>
                {result.meetings.map((meeting) => (
                  <tr key={meeting.meetingKey} className="border-b border-(--color-border) last:border-0">
                    <td className="py-1 pr-2">{meeting.meetingName}</td>
                    <td className="py-1">{IngestOutcomeLabel[meeting.outcome]}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </Card>
  );
}
