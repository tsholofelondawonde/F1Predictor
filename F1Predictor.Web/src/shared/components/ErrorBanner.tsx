interface ErrorBannerProps {
  title: string;
  message: string;
  action?: { label: string; href: string };
}

export function ErrorBanner({ title, message, action }: ErrorBannerProps) {
  return (
    <div className="rounded-md border border-(--color-error-border) bg-(--color-error-bg) p-4 text-sm">
      <p className="font-semibold text-(--color-error-text)">{title}</p>
      <p className="mt-1 text-(--color-error-text)">{message}</p>
      {action && (
        <a href={action.href} className="mt-2 inline-block font-medium text-(--color-accent) underline">
          {action.label}
        </a>
      )}
    </div>
  );
}
