import type { Metadata } from "next";
import { Header } from "@/shared/components/Header";

export const metadata: Metadata = {
  title: "Dashboard",
};

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <Header />
      <main className="mx-auto w-full max-w-4xl flex-1 px-6 py-8">{children}</main>
    </>
  );
}
