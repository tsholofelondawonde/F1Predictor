import Link from "next/link";

export function Header() {
  return (
    <header className="border-b border-(--color-border) px-6 py-4">
      <div className="mx-auto flex max-w-4xl items-center justify-between">
        <Link href="/" className="text-lg font-semibold">
          F1 Race Predictor
        </Link>
        <nav className="flex gap-4 text-sm">
          <Link href="/" className="hover:underline">
            Dashboard
          </Link>
          <Link href="/holdout" className="hover:underline">
            Holdout
          </Link>
        </nav>
      </div>
    </header>
  );
}
