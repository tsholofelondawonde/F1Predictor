import type { ButtonHTMLAttributes } from "react";

export type ButtonVariant = "primary" | "secondary";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
}

/**
 * Shared button visual styling, exposed so navigational CTAs can apply it to a
 * `next/link` `<Link>` instead — nesting a `<button>` inside an `<a>` is invalid HTML.
 */
export function buttonClasses(variant: ButtonVariant = "primary", className?: string): string {
  const base =
    "inline-flex items-center justify-center rounded-(--radius) px-4 py-2 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent) active:scale-[0.98]";
  const variantClass =
    variant === "primary"
      ? "bg-(--color-accent) text-(--color-on-accent) hover:bg-(--color-accent-hover)"
      : "border border-(--color-border) text-(--color-foreground) hover:bg-(--color-surface-hover)";

  return `${base} ${variantClass} ${className ?? ""}`;
}

export function Button({ variant = "primary", className, ...props }: ButtonProps) {
  return <button className={buttonClasses(variant, className)} {...props} />;
}
