import axios, {
  AxiosError,
  AxiosInstance,
  InternalAxiosRequestConfig,
} from 'axios';
import { useAuthStore } from './auth/use-auth-store';
import authClient from './auth-client';
import { API_ENDPOINTS, PAGE_ROUTES } from './routes';

const captureEvent = (event: string, props: Record<string, unknown>) => {
  if (typeof window !== 'undefined') {
    import('posthog-js')
      .then(({ default: posthog }) => posthog.capture(event, props))
      .catch(() => {});
  }
};

// --- Configuration ---

const MAX_RETRIES = 2;
const RETRY_DELAY_MS = 1000;
const RETRYABLE_STATUS_CODES = [408, 429, 500, 502, 503, 504];

// --- API Client ---

const apiClient: AxiosInstance = axios.create({
  baseURL: '',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// --- Request Interceptor: Attach Auth Token ---

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const { accessToken, userId } = useAuthStore.getState();

    if (!accessToken && userId) {
      console.warn('[api-client] Request with userId but no accessToken', {
        userId,
        url: config.url,
      });
      captureEvent('auth_inconsistent_state', {
        userId,
        url: config.url,
        method: config.method,
      });
    }

    if (accessToken && config.headers) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// --- Response Interceptor: Handle Errors & Token Refresh ---

let isRefreshing = false;
// Queue of requests waiting for token refresh to complete
// On success: resolve with new token; on failure: reject with error
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token?: string) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else if (token) {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

const logRefreshFailure = (error: unknown, userId: string | null) => {
  console.error('[api-client] Token refresh failed', {
    userId,
    error: error instanceof Error ? error.message : String(error),
    timestamp: new Date().toISOString(),
  });
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _tokenRefreshAttempted?: boolean;
      _retryCount?: number;
    };

    if (!originalRequest) {
      return Promise.reject(error);
    }

    // Skip retry/refresh for auth endpoints to avoid loops
    const isAuthEndpoint = originalRequest.url?.includes('/auth/');

    // --- Token Refresh on 401 ---
    if (
      error.response?.status === 401 &&
      !originalRequest._tokenRefreshAttempted &&
      !isAuthEndpoint
    ) {
      const { refreshToken, userId } = useAuthStore.getState();

      // Early exit if no refresh token - logout immediately
      if (!refreshToken) {
        console.warn(
          '[api-client] 401 with no refreshToken — redirecting to login',
          {
            userId,
            url: originalRequest.url,
          },
        );
        captureEvent('auth_no_refresh_token', {
          userId,
          url: originalRequest.url,
          method: originalRequest.method,
        });

        useAuthStore.setState({
          userId: null,
          accessToken: null,
          refreshToken: null,
          requiredAction: null,
        });
        if (typeof window !== 'undefined') {
          window.location.href = PAGE_ROUTES.LOGIN();
        }
        return Promise.reject(new Error('No refresh token. Session expired'));
      }

      // Queue this request if refresh is already in progress
      if (isRefreshing) {
        return new Promise<string>((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((token) => {
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${token}`;
          }
          return apiClient(originalRequest);
        });
      }

      originalRequest._tokenRefreshAttempted = true;
      isRefreshing = true;

      try {
        const response = await authClient.post(
          API_ENDPOINTS.AUTH.RENEW_TOKENS,
          {
            refreshToken,
          },
          { timeout: 10000 },
        );

        const { accessToken, refreshToken: newRefreshToken } =
          response.data.result;

        if (!accessToken || !newRefreshToken) {
          throw new Error(
            `Refresh returned incomplete tokens: access=${!!accessToken} refresh=${!!newRefreshToken}`,
          );
        }

        useAuthStore.setState({
          accessToken,
          refreshToken: newRefreshToken,
        });

        captureEvent('auth_tokens_refreshed', {
          userId,
          hasAccessToken: !!accessToken,
          hasRefreshToken: !!newRefreshToken,
        });

        processQueue(null, accessToken);

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        }
        return apiClient(originalRequest);
      } catch (refreshError) {
        const errorMsg =
          refreshError instanceof Error
            ? refreshError.message
            : String(refreshError);
        console.error('[api-client] Refresh/retry failed', {
          userId,
          error: errorMsg,
        });
        captureEvent('auth_refresh_failed', {
          userId,
          error: errorMsg,
        });
        logRefreshFailure(refreshError, userId);
        processQueue(refreshError);
        useAuthStore.setState({
          userId: null,
          accessToken: null,
          refreshToken: null,
          requiredAction: null,
        });
        if (typeof window !== 'undefined') {
          window.location.href = PAGE_ROUTES.LOGIN();
        }
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    // --- Retry on Transient Errors (GET only) ---
    const retryCount = originalRequest._retryCount || 0;
    const isGetRequest = originalRequest.method?.toLowerCase() === 'get';
    const isRetryable =
      isGetRequest &&
      ((error.response &&
        RETRYABLE_STATUS_CODES.includes(error.response.status)) ||
        error.code === 'ECONNABORTED' ||
        error.code === 'ERR_NETWORK');

    if (isRetryable && retryCount < MAX_RETRIES && !isAuthEndpoint) {
      originalRequest._retryCount = retryCount + 1;

      const delay = RETRY_DELAY_MS * Math.pow(2, retryCount);
      await new Promise((resolve) => setTimeout(resolve, delay));

      return apiClient(originalRequest);
    }

    return Promise.reject(error);
  },
);

export const setAcceptLanguage = (locale: string) => {
  if (apiClient.defaults.headers.common['Accept-Language'] !== locale) {
    apiClient.defaults.headers.common['Accept-Language'] = locale;
    console.log(`apiClient Accept-Language updated to: ${locale}`);
  }
};

export const setTimezone = (timezone: string) => {
  if (apiClient.defaults.headers.common['X-Timezone-Id'] !== timezone) {
    apiClient.defaults.headers.common['X-Timezone-Id'] = timezone;
    console.log(`apiClient X-Timezone-Id updated to: ${timezone}`);
  }
};

export default apiClient;
