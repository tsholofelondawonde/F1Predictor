import type { AxiosError } from "axios";
import type { ProblemDetails, ProblemDetailsValidationError } from "@/shared/types/problem-details";

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly userMessage: string;
  readonly validationErrors?: ProblemDetailsValidationError[];

  constructor(status: number, code: string, userMessage: string, validationErrors?: ProblemDetailsValidationError[]) {
    super(userMessage);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.userMessage = userMessage;
    this.validationErrors = validationErrors;
  }

  static fromAxiosError(error: AxiosError<ProblemDetails>): ApiError {
    const problem = error.response?.data;

    if (problem) {
      return new ApiError(
        problem.status ?? error.response?.status ?? 0,
        problem.title ?? "Unknown",
        problem.userMessage ?? problem.detail ?? error.message,
        problem.errors,
      );
    }

    // A response with no readable body still means the server answered, so this is not a
    // connectivity problem — falling through to the "Network" message below would send the
    // reader looking in entirely the wrong place. Key off the status instead.
    if (error.response) {
      const { status, headers } = error.response;

      return new ApiError(status, `Http${status}`, messageForBodilessStatus(status, headers?.["retry-after"]));
    }

    if (error.code === "ECONNABORTED") {
      return new ApiError(0, "Timeout", "The request took too long to respond.");
    }

    return new ApiError(0, "Network", "Could not reach the server. Check your connection.");
  }
}

function messageForBodilessStatus(status: number, retryAfter: unknown): string {
  if (status === 429) {
    const seconds = Number(retryAfter);

    if (!Number.isFinite(seconds) || seconds <= 0) {
      return "Too many requests. Wait a moment and try again.";
    }

    const minutes = Math.ceil(seconds / 60);
    const wait = seconds >= 60
      ? `${minutes} ${minutes === 1 ? "minute" : "minutes"}`
      : `${Math.ceil(seconds)} seconds`;

    return `Too many requests. Try again in ${wait}.`;
  }

  if (status === 401 || status === 403) {
    return "This action needs a valid API key.";
  }

  if (status >= 500) {
    return "The server failed to handle the request.";
  }

  return `The request failed (HTTP ${status}).`;
}
