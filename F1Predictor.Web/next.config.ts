import type { NextConfig } from "next";

// The public NEXT_PUBLIC_* values are supplied by the host: Aspire injects
// NEXT_PUBLIC_API_URL locally, .env.local covers the rest, and Vercel sets all three as
// project environment variables. Next inlines them into the bundle at build time on its
// own, so nothing needs restating here.

const nextConfig: NextConfig = {
  // Standalone output is for a container image. Vercel builds its own output and its trace
  // step fails on a standalone build ("no such file ... next-server.js.nft.json"), so emit
  // it everywhere except there.
  ...(process.env.VERCEL ? {} : { output: "standalone" as const }),
};

export default nextConfig;
