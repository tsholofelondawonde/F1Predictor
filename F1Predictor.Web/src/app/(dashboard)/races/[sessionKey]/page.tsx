import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getRacePredictions } from "@/features/predictions/predictions-service";
import { RacePredictionsView } from "@/features/predictions/components/RacePredictionsView";

export async function generateMetadata({
  params,
}: PageProps<"/races/[sessionKey]">): Promise<Metadata> {
  const { sessionKey } = await params;
  const key = Number(sessionKey);

  if (!Number.isInteger(key)) {
    return { title: "Race Predictions" };
  }

  try {
    const data = await getRacePredictions(key);
    return {
      title: data.meetingName,
      description: `Podium and points-finish probabilities for ${data.meetingName}.`,
    };
  } catch {
    return { title: "Race Predictions" };
  }
}

export default async function RacePredictionsPage({ params }: PageProps<"/races/[sessionKey]">) {
  const { sessionKey } = await params;
  const key = Number(sessionKey);

  if (!Number.isInteger(key)) {
    notFound();
  }

  return <RacePredictionsView sessionKey={key} />;
}
