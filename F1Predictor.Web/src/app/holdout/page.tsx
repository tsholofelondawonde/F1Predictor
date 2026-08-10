import { HoldoutView } from "@/features/predictions/components/HoldoutView";

export default async function HoldoutPage({ searchParams }: PageProps<"/holdout">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <HoldoutView year={year} />;
}
