import { RacePredictionsView } from "@/features/predictions/components/RacePredictionsView";

export default async function RacePredictionsPage({ params }: PageProps<"/races/[sessionKey]">) {
  const { sessionKey } = await params;

  return <RacePredictionsView sessionKey={Number(sessionKey)} />;
}
