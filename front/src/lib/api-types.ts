export interface ApiError {
  code: string;
  error: string;
}

export interface ApiResult<T> {
  success: boolean;
  result: T;
  errors: ApiError[];
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
}
