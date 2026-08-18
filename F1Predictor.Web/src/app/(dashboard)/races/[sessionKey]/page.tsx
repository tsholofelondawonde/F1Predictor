import type { Metadata } from "next";
import { RacePredictionsView } from "@/features/predictions/components/RacePredictionsView";

export const metadata: Metadata = {
  title: "Race Predictions",
  description: "Podium and points-finish probabilities for this race weekend.",
};

export default async function RacePredictionsPage({ params }: PageProps<"/races/[sessionKey]">) {
  const { sessionKey } = await params;

  return <RacePredictionsView sessionKey={Number(sessionKey)} />;
}
