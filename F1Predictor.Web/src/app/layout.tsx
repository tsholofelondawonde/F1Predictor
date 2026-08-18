import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000";
const description =
  "GridMind trains podium and points-finish predictors, and simulates F1 championships, on live OpenF1 data.";

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default: "GridMind",
    template: "%s · GridMind",
  },
  description,
  openGraph: {
    title: "GridMind",
    description,
    url: siteUrl,
    siteName: "GridMind",
    type: "website",
  },
  twitter: {
    card: "summary_large_image",
    title: "GridMind",
    description,
  },
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">{children}</body>
    </html>
  );
}
