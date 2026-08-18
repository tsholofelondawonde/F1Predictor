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

    if (error.code === "ECONNABORTED") {
      return new ApiError(0, "Timeout", "The request took too long to respond.");
    }

    return new ApiError(0, "Network", "Could not reach the server. Check your connection.");
  }
}
