interface ErrorBannerProps {
  title: string;
  message: string;
  action?: { label: string; href: string };
}

export function ErrorBanner({ title, message, action }: ErrorBannerProps) {
  return (
    <div className="rounded-(--radius) border border-(--color-error-border) border-l-4 bg-(--color-error-bg) p-4 text-sm">
      <p className="font-mono text-xs font-semibold uppercase tracking-wider text-(--color-error-text)">{title}</p>
      <p className="mt-1 text-(--color-error-text)">{message}</p>
      {action && (
        <a
          href={action.href}
          className="mt-2 inline-block font-medium text-(--color-accent) underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent)"
        >
          {action.label}
        </a>
      )}
    </div>
  );
}
