"use client";

import type { ReactNode } from "react";
import { useAiStatus } from "@/shared/lib/use-ai-status";

/**
 * Renders its children only when the API reports a chat provider. With the AI layer paused
 * (`Ai:Enabled=false`) or unconfigured, AI-only surfaces disappear entirely rather than
 * showing an empty card. Gating from outside also keeps a child's hooks unconditional.
 */
export function WhenAiAvailable({ children }: { children: ReactNode }) {
  const { chatAvailable } = useAiStatus();

  return chatAvailable ? children : null;
}
