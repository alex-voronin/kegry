export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  venueId?: string;
  [key: string]: unknown;
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
}

export interface PresignedUrlResponse {
  uploadUrl: string;
  publicDownloadUrl: string;
}
