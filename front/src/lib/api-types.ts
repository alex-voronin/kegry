export interface ApiError {
  code: string;
  error: string;
}

export interface ApiResult<T> {
  success: boolean;
  result: T;
  errors: ApiError[];
}

export interface ApiProblem {
  code?: string;
  detail: string;
  target?: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  venueId?: string;
  errors?: Record<string, string[]> | ApiProblem[];
  [key: string]: unknown;
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
}
