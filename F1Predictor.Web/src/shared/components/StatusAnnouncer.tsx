interface StatusAnnouncerProps {
  message: string | null;
}

/** Visually hidden live region announcing an async action's completion to screen readers. */
export function StatusAnnouncer({ message }: StatusAnnouncerProps) {
  return (
    <div role="status" aria-live="polite" className="sr-only">
      {message}
    </div>
  );
}
