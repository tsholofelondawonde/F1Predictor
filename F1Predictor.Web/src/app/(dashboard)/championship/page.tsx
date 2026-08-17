import { ChampionshipView } from "@/features/championship/components/ChampionshipView";

export default async function ChampionshipPage({ searchParams }: PageProps<"/championship">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <ChampionshipView year={year} />;
}
