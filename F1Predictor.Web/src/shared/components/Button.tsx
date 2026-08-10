import type { ButtonHTMLAttributes } from "react";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary";
}

export function Button({ variant = "primary", className, ...props }: ButtonProps) {
  const base = "rounded-md px-4 py-2 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50";
  const variantClass =
    variant === "primary"
      ? "bg-(--color-accent) text-white hover:bg-(--color-accent-hover)"
      : "border border-(--color-border) text-(--color-foreground) hover:bg-(--color-surface-hover)";

  return <button className={`${base} ${variantClass} ${className ?? ""}`} {...props} />;
}
