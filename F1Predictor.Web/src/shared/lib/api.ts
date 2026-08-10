import axios from "axios";
import type { ProblemDetails } from "@/shared/types/problem-details";
import { ApiError } from "@/shared/lib/api-error";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5286";

// GET /health returns plain JSON, not ProblemDetails — don't route health checks through
// this instance's interceptor, which assumes every error body is a ProblemDetails.
export const api = axios.create({ baseURL: `${API_URL}/api` });

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError<ProblemDetails>(error)) {
      return Promise.reject(ApiError.fromAxiosError(error));
    }

    return Promise.reject(error);
  },
);
