import { Hero } from "@/features/landing/components/Hero";
import { ProofStrip } from "@/features/landing/components/ProofStrip";
import { FeatureSection } from "@/features/landing/components/FeatureSection";
import { HowItWorks } from "@/features/landing/components/HowItWorks";
import { FinalCTA } from "@/features/landing/components/FinalCTA";
import { FlagIcon, TrendingUpIcon, DiceIcon } from "@/features/landing/components/icons";

export default function LandingPage() {
  return (
    <>
      <Hero />
      <ProofStrip />

      <section id="features" className="mx-auto max-w-6xl px-6 py-20">
        <h2 className="font-display text-3xl tracking-wide">What GridMind predicts</h2>

        <div className="mt-10 grid gap-12 sm:grid-cols-2 lg:grid-cols-3">
          <FeatureSection
            icon={<FlagIcon className="h-6 w-6" />}
            title="Race Predictions"
            description="Podium and points-finish probability for every driver, from five features: grid position, gap to pole, pit stop count, average pit stop duration, and rainfall — trained per season with ML.NET's SDCA logistic regression."
          />
          <FeatureSection
            icon={<TrendingUpIcon className="h-6 w-6" />}
            title="Championship Forecasting"
            description="A Plackett-Luce driver strength model feeds a 10,000-run Monte Carlo season simulation, producing title probabilities for both the Drivers' and Constructors' championships."
          />
          <FeatureSection
            icon={<DiceIcon className="h-6 w-6" />}
            title="What-If Scenarios"
            description="See exactly what a contender needs to stay alive and what the leader needs to clinch — points to close the gap, required average finish, the math behind every 'still alive' badge."
          />
        </div>
      </section>

      <HowItWorks />
      <FinalCTA />
    </>
  );
}
