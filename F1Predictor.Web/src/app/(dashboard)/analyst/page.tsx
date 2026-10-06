import type { Metadata } from "next";
import { AnalystChat } from "@/features/analysis/components/AnalystChat";

const title = "Analyst";
const description =
  "Ask GridMind's AI analyst about the next race, the standings and the title odds — every number comes from the models.";

export const metadata: Metadata = {
  title,
  description,
  openGraph: { title, description },
  twitter: { title, description },
};

export default async function AnalystPage({ searchParams }: PageProps<"/analyst">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <AnalystChat year={year} />;
}
