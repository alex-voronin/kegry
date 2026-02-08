import axios from 'axios';

/**
 * Extracts an error message from various error types (axios, API response, generic).
 */
export function extractApiError(
  error: unknown,
  fallback = 'An error occurred',
): string {
  if (axios.isAxiosError(error)) {
    // Try to get error from API response structure
    const apiError = error.response?.data?.errors?.[0]?.error;
    if (apiError) return apiError;

    // Fallback to axios error message
    return error.message || fallback;
  }

  if (error instanceof Error) {
    return error.message || fallback;
  }

  if (typeof error === 'string') {
    return error;
  }

  return fallback;
}

/**
 * Extracts error message and throws it as a new Error.
 * Use in catch blocks for consistent error handling.
 */
export function handleApiError(
  error: unknown,
  fallback = 'An error occurred',
): never {
  throw new Error(extractApiError(error, fallback));
}
