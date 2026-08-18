import type { Metadata } from "next";
import { NextRaceView } from "@/features/predictions/components/NextRaceView";

export const metadata: Metadata = {
  title: "Next Race",
  description: "Podium and points-finish probabilities for the upcoming Grand Prix, updated as the grid firms up.",
};

export default async function NextRacePage({ searchParams }: PageProps<"/next-race">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <NextRaceView year={year} />;
}
