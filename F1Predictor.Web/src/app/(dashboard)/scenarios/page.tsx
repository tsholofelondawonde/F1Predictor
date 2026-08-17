import { ScenariosView } from "@/features/championship/components/ScenariosView";

export default async function ScenariosPage({ searchParams }: PageProps<"/scenarios">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <ScenariosView year={year} />;
}
