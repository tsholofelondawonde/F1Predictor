import type { Metadata } from "next";
import { YearSelector } from "@/features/seasons/components/YearSelector";
import { IngestSeasonPanel } from "@/features/seasons/components/IngestSeasonPanel";
import { RebuildFeaturesButton } from "@/features/seasons/components/RebuildFeaturesButton";
import { RaceList } from "@/features/seasons/components/RaceList";
import { TrainModelsPanel } from "@/features/training/components/TrainModelsPanel";

const title = "Dashboard";
const description =
  "Ingest a season, rebuild features, train podium and points-finish models, and browse ingested races.";

export const metadata: Metadata = {
  title,
  description,
  openGraph: { title, description },
  twitter: { title, description },
};

export default function DashboardPage() {
  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold">Dashboard</h1>
          <p className="text-sm text-(--color-muted)">
            Ingest a season, rebuild features, train models, then view predictions. Steps are independent —
            re-running an earlier step is safe, and you can jump ahead, but predictions won&apos;t work until
            the season is ingested and models are trained.
          </p>
        </div>
        <YearSelector />
      </div>

      <IngestSeasonPanel />
      <RebuildFeaturesButton />
      <TrainModelsPanel />
      <RaceList />
    </div>
  );
}
