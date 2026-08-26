import type { NextConfig } from "next";

// Vercel has no per-project env vars set for this deployment, and vercel.json's build.env
// is not applied to the Next build, so both public values are resolved here instead. An
// explicitly set environment variable still wins in every case.

// Where the API lives. The default is the deployed Azure Container App; override with
// NEXT_PUBLIC_API_URL for local work (Aspire injects it) or a different environment.
const apiUrl =
  process.env.NEXT_PUBLIC_API_URL ??
  "https://ca-grid-mind-api-dev-001.redcliff-ef452535.germanywestcentral.azurecontainerapps.io";

// The public origin is only known at build time on Vercel, so fall back to the project's
// production domain. Without this, metadataBase, robots.ts and sitemap.ts silently emit
// localhost URLs in production.
const siteUrl =
  process.env.NEXT_PUBLIC_SITE_URL ??
  (process.env.VERCEL_PROJECT_PRODUCTION_URL
    ? `https://${process.env.VERCEL_PROJECT_PRODUCTION_URL}`
    : undefined);

const nextConfig: NextConfig = {
  // Standalone output is for a container image. Vercel builds its own output and its trace
  // step fails on a standalone build ("no such file ... next-server.js.nft.json"), so emit
  // it everywhere except there.
  ...(process.env.VERCEL ? {} : { output: "standalone" as const }),
  env: {
    NEXT_PUBLIC_API_URL: apiUrl,
    ...(siteUrl ? { NEXT_PUBLIC_SITE_URL: siteUrl } : {}),
  },
};

export default nextConfig;
