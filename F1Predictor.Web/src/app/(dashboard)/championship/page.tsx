import type { Metadata } from "next";
import { ChampionshipView } from "@/features/championship/components/ChampionshipView";

const title = "Championship";
const description =
  "Live driver and constructor championship forecasts, simulated from current standings and remaining races.";

export const metadata: Metadata = {
  title,
  description,
  openGraph: { title, description },
  twitter: { title, description },
};

export default async function ChampionshipPage({ searchParams }: PageProps<"/championship">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <ChampionshipView year={year} />;
}
