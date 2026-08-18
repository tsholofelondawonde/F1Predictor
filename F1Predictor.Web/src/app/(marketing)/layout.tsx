import type { Metadata } from "next";
import { Bebas_Neue } from "next/font/google";
import { MarketingNav } from "@/shared/components/MarketingNav";
import { MarketingFooter } from "@/shared/components/MarketingFooter";

const bebasNeue = Bebas_Neue({
  weight: "400",
  variable: "--font-bebas-neue",
  subsets: ["latin"],
  display: "swap",
});

export const metadata: Metadata = {
  title: { absolute: "GridMind — F1 Race & Championship Predictions" },
  description:
    "Podium probabilities, points-finish odds, and Monte Carlo championship forecasts built on live OpenF1 data — with honest, imbalanced-class-aware model metrics.",
};

export default function MarketingLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className={`${bebasNeue.variable} flex min-h-full flex-1 flex-col`}>
      <MarketingNav />
      <main className="flex-1">{children}</main>
      <MarketingFooter />
    </div>
  );
}
