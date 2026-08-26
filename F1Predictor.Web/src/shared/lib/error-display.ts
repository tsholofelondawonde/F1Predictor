import type { ApiError } from "@/shared/lib/api-error";

export interface ErrorDisplay {
  title: string;
  message: string;
}

/**
 * Maps an ApiError to display copy by HTTP status, for callers that haven't already
 * matched on a known backend error code. "Network"/"Timeout" (status 0) are pre-curated
 * at the source in ApiError.fromAxiosError, so those branches just relay that copy.
 */
export function getErrorDisplay(error: ApiError): ErrorDisplay {
  if (error.status === 0 && error.code === "Timeout") {
    return { title: "Request timed out", message: error.userMessage };
  }

  if (error.status === 0 && error.code === "Network") {
    return { title: "Can't reach the server", message: error.userMessage };
  }

  switch (error.status) {
    case 400:
      return { title: "Invalid request", message: "That request wasn't valid. Double-check your input and try again." };
    case 401:
      return { title: "Sign-in required", message: "You need to sign in to do that." };
    case 403:
      return { title: "Not allowed", message: "You don't have permission to do that." };
    case 404:
      return { title: "Not found", message: "We couldn't find what you were looking for." };
    case 409:
      return { title: "Conflict", message: "That action conflicts with the current state — refresh and try again." };
    case 429:
      return { title: "Too many requests", message: "You're doing that too often. Wait a moment and try again." };
    case 500:
      return { title: "Server error", message: "Something went wrong on our end. Try again shortly." };
    default:
      return { title: "Something went wrong", message: error.userMessage };
  }
}
