import type { Metadata } from "next";
import { ScenariosView } from "@/features/championship/components/ScenariosView";

export const metadata: Metadata = {
  title: "Title Scenarios",
  description: "What each championship contender needs to happen to win the title, with simulated title chances.",
};

export default async function ScenariosPage({ searchParams }: PageProps<"/scenarios">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <ScenariosView year={year} />;
}
