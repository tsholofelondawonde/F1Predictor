export interface ProblemDetailsValidationError {
  code: string;
  description: string;
  userMessage: string;
}

export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  userMessage: string;
  errors?: ProblemDetailsValidationError[];
}
