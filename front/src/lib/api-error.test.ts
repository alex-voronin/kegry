import { describe, expect, it } from 'vitest';
import { ApiRequestError, extractApiError, rethrowApiError } from './api-error';
import type { ProblemDetails } from './api-types';

function problemError(
  data: ProblemDetails,
  message = 'Request failed with status code 422',
) {
  return Object.assign(new Error(message), {
    isAxiosError: true,
    response: { data, status: data.status ?? 422 },
  });
}

describe('extractApiError', () => {
  it('extracts detail, code and context from ProblemDetails', () => {
    const problem: ProblemDetails = {
      type: 'https://httpstatuses.io/422',
      title: 'Unprocessable Entity',
      status: 422,
      detail: 'You must be in the venue to check in',
      code: 'OutsideVenueRadius',
      venueId: '0199f0a0-7d3a-7a4e-9d0a-2b7e9f8c1d23',
    };

    const [message, code, context] = extractApiError(problemError(problem));

    expect(message).toBe('You must be in the venue to check in');
    expect(code).toBe('OutsideVenueRadius');
    expect(context).toEqual(problem);
    expect(context?.venueId).toBe('0199f0a0-7d3a-7a4e-9d0a-2b7e9f8c1d23');
  });

  it('falls back to title when detail is missing', () => {
    const problem: ProblemDetails = {
      title: 'Not Found',
      status: 404,
      code: 'VenueNotFound',
    };

    const [message, code, context] = extractApiError(
      problemError(problem, '404'),
    );

    expect(message).toBe('Not Found');
    expect(code).toBe('VenueNotFound');
    expect(context).toEqual(problem);
  });

  it('keeps validation errors in context', () => {
    const problem: ProblemDetails = {
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: {
        Email: ['Email is invalid'],
        Password: ['Password is too short'],
      },
    };

    const [message, code, context] = extractApiError(
      problemError(problem, '400'),
    );

    expect(message).toBe('One or more validation errors occurred.');
    expect(code).toBe('unknown');
    expect(context?.errors).toEqual({
      Email: ['Email is invalid'],
      Password: ['Password is too short'],
    });
  });

  it('returns axios message when response has no detail or title', () => {
    const [message, code, context] = extractApiError(
      problemError({}, 'Network Error'),
    );

    expect(message).toBe('Network Error');
    expect(code).toBe('unknown');
    expect(context).toBeUndefined();
  });

  it('returns axios message for network errors without response', () => {
    const error = Object.assign(new Error('Network Error'), {
      isAxiosError: true,
    });

    const [message, code, context] = extractApiError(error);

    expect(message).toBe('Network Error');
    expect(code).toBe('unknown');
    expect(context).toBeUndefined();
  });

  it('extracts ApiRequestError message and code', () => {
    const [message, code, context] = extractApiError(
      new ApiRequestError('Invalid PIN-code', 'InvalidPinCode'),
    );

    expect(message).toBe('Invalid PIN-code');
    expect(code).toBe('InvalidPinCode');
    expect(context).toBeUndefined();
  });

  it('extracts generic Error message', () => {
    const [message, code] = extractApiError(new Error('Something failed'));

    expect(message).toBe('Something failed');
    expect(code).toBe('unknown');
  });

  it('returns string errors as-is', () => {
    const [message, code] = extractApiError('Raw failure');

    expect(message).toBe('Raw failure');
    expect(code).toBe('unknown');
  });

  it('returns fallback for unknown values', () => {
    const [message, code, context] = extractApiError({}, 'Fallback message');

    expect(message).toBe('Fallback message');
    expect(code).toBe('unknown');
    expect(context).toBeUndefined();
  });

  it('uses fallback when axios message is empty', () => {
    const error = Object.assign(new Error(), {
      isAxiosError: true,
      message: '',
    });

    const [message] = extractApiError(error, 'Fallback message');

    expect(message).toBe('Fallback message');
  });
});

describe('rethrowApiError', () => {
  it('throws ApiRequestError with extracted message and code', () => {
    const problem: ProblemDetails = {
      status: 403,
      detail: 'Invalid PIN-code',
      code: 'InvalidPinCode',
    };

    expect(() => rethrowApiError(problemError(problem))).toThrowError(
      new ApiRequestError('Invalid PIN-code', 'InvalidPinCode'),
    );
  });
});
