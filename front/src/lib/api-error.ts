import axios from 'axios';

export class ApiRequestError extends Error {
  code?: string;
  constructor(message: string, code?: string) {
    super(message);
    this.name = 'ApiRequestError';
    this.code = code;
  }
}

/**
 * Extracts an error message from various error types (axios, API response, generic).
 */
export function extractApiError(
  error: unknown,
  fallback = 'An error occurred',
): [string, string] {
  if (axios.isAxiosError(error)) {
    const apiError = error.response?.data?.errors?.[0];
    if (apiError) {
      return [apiError.error, apiError.code];
    }

    // Fallback to axios error message
    return [error.message || fallback, 'unknown'];
  }

  if (error instanceof Error) {
    return [error.message || fallback, 'unknown'];
  }

  if (typeof error === 'string') {
    return [error, 'unknown'];
  }

  return [fallback, 'unknown'];
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
