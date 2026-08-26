import type { NextConfig } from "next";

// The public origin is only known at build time on Vercel, so fall back to the project's
// production domain when NEXT_PUBLIC_SITE_URL is not set explicitly. Without this,
// metadataBase, robots.ts and sitemap.ts silently emit localhost URLs in production.
const siteUrl =
  process.env.NEXT_PUBLIC_SITE_URL ??
  (process.env.VERCEL_PROJECT_PRODUCTION_URL
    ? `https://${process.env.VERCEL_PROJECT_PRODUCTION_URL}`
    : undefined);

const nextConfig: NextConfig = {
  output: "standalone",
  ...(siteUrl ? { env: { NEXT_PUBLIC_SITE_URL: siteUrl } } : {}),
};

export default nextConfig;
