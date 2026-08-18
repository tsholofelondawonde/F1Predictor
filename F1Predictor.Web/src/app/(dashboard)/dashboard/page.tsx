import type { Metadata } from "next";
import { YearSelector } from "@/features/seasons/components/YearSelector";
import { IngestSeasonPanel } from "@/features/seasons/components/IngestSeasonPanel";
import { RebuildFeaturesButton } from "@/features/seasons/components/RebuildFeaturesButton";
import { RaceList } from "@/features/seasons/components/RaceList";
import { TrainModelsPanel } from "@/features/training/components/TrainModelsPanel";

export const metadata: Metadata = {
  title: "Dashboard",
  description: "Ingest a season, rebuild features, train podium and points-finish models, and browse ingested races.",
};

export default function DashboardPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
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
