import type { Metadata } from "next";
import { HoldoutView } from "@/features/predictions/components/HoldoutView";

export const metadata: Metadata = {
  title: "Holdout Predictions",
  description: "Model predictions for the most recent race, held out of training, shown against actual results.",
};

export default async function HoldoutPage({ searchParams }: PageProps<"/holdout">) {
  const params = await searchParams;
  const yearParam = Array.isArray(params.year) ? params.year[0] : params.year;
  const year = yearParam ? Number(yearParam) : new Date().getFullYear();

  return <HoldoutView year={year} />;
}
