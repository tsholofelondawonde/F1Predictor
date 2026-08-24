import type { Metadata } from "next";
import { ScenariosView } from "@/features/championship/components/ScenariosView";

const title = "Title Scenarios";
const description =
  "What each championship contender needs to happen to win the title, with simulated title chances.";

export const metadata: Metadata = {
  title,
  description,
  openGraph: { title, description },
  twitter: { title, description },
};

export default async function ScenariosPage({ searchParams }: PageProps<"/scenarios">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <ScenariosView year={year} />;
}
