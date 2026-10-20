import axios from 'axios';
import type { ProblemDetails } from './api-types';

export class ApiRequestError extends Error {
  code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = 'ApiRequestError';
    this.code = code;
  }
}

/**
 * Extracts an error message, code and context from various error types
 * (ProblemDetails from the API, axios, generic errors).
 */
export function extractApiError(
  error: unknown,
  fallback = 'An error occurred',
): [string, string, ProblemDetails?] {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ProblemDetails | undefined;

    if (data?.detail || data?.title) {
      return [
        data.detail ?? data.title ?? fallback,
        data.code ?? 'unknown',
        data,
      ];
    }

    return [error.message || fallback, 'unknown', undefined];
  }

  if (error instanceof ApiRequestError) {
    return [error.message, error.code || 'unknown', undefined];
  }

  if (error instanceof Error) {
    return [error.message || fallback, 'unknown', undefined];
  }

  if (typeof error === 'string') {
    return [error, 'unknown', undefined];
  }

  return [fallback, 'unknown', undefined];
}

/**
 * Extracts error message and throws it as a new Error.
 * Use in catch blocks for consistent error handling.
 */
export function rethrowApiError(
  error: unknown,
  fallback = 'An error occurred',
): never {
  const [message, code] = extractApiError(error, fallback);
  throw new ApiRequestError(message, code);
}
