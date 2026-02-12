'use client';

import { create } from 'zustand';
import axios from 'axios';
import { persist } from 'zustand/middleware';
import { API_ENDPOINTS, PAGE_ROUTES } from '../routes';
import { rethrowApiError } from '../api-error';

// Note: Auth endpoints use raw axios to avoid token interceptor issues during login/signup.
// For other API calls, use apiClient from '../api-client' for automatic token handling and retries.

// --- Types ---

export interface ApiError {
  code: string;
  error: string;
}

export interface ApiResult<T> {
  success: boolean;
  result: T;
  errors: ApiError[];
}

export enum RequiredAction {
  None = 'None',
  EmailVerification = 'EmailVerification',
  OnboardingCompletion = 'OnboardingCompletion',
}

export interface AuthResponse {
  userId: string;
  requiredAction: RequiredAction;
  accessToken: string | null;
  refreshToken: string | null;
}

export interface SigninResponse {
  isSignupRequired: boolean;
  auth: AuthResponse | null;
}

export interface SignInRequest {
  email: string;
  password: string;
}

export interface RenewTokensRequest {
  refreshToken: string;
}

export interface RequestPasswordResetRequest {
  email: string;
}

export interface ConfirmPasswordResetRequest {
  encodedEmail: string;
  encodedToken: string;
  newPassword: string;
}

export const hasCompleteOnboarding = (state: AuthState): boolean =>
  !!state.accessToken && state.requiredAction === RequiredAction.None;

interface AuthState {
  userId: string | null;
  accessToken: string | null;
  refreshToken: string | null;
  requiredAction: RequiredAction | null;
  hasHydrated: boolean;

  setHasHydrated: (hydrated: boolean) => void;

  signin: (email: string, password: string) => Promise<AuthResponse>;
  signup: (email: string, password: string) => Promise<AuthResponse>;
  signinWithGoogle: (idToken: string) => Promise<AuthResponse>;
  confirmEmail: (userId: string, token: string) => Promise<AuthResponse>;
  logout: () => Promise<void>;
  requestPasswordReset: (email: string) => Promise<void>;
  confirmPasswordReset: (
    encodedEmail: string,
    encodedToken: string,
    newPassword: string,
  ) => Promise<void>;

  // Internal/Helper action to update tokens
  setAuth: (auth: AuthResponse) => void;
  setRequiredAction: (action: RequiredAction) => void;
}

// --- Store ---

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      userId: null,
      accessToken: null,
      refreshToken: null,
      requiredAction: null,
      hasHydrated: false,

      setHasHydrated: (hydrated: boolean) => {
        set({ hasHydrated: hydrated });
      },

      setAuth: (auth: AuthResponse) => {
        set({
          userId: auth.userId,
          accessToken: auth.accessToken,
          refreshToken: auth.refreshToken,
          requiredAction: auth.requiredAction,
        });
      },

      setRequiredAction: (action: RequiredAction) => {
        set({ requiredAction: action });
      },

      signin: async (email, password) => {
        try {
          const signinRes = await axios.post<ApiResult<SigninResponse>>(
            API_ENDPOINTS.AUTH.SIGNIN,
            { email, password },
          );

          if (!signinRes.data.success) {
            throw new Error(signinRes.data.errors[0]?.error || 'Signin failed');
          }

          const { isSignupRequired, auth } = signinRes.data.result;

          if (isSignupRequired) {
            throw new Error('Account not found. Please sign up first.');
          }

          if (!auth) {
            throw new Error('Unexpected auth state');
          }

          get().setAuth(auth);
          return auth;
        } catch (error) {
          rethrowApiError(error, 'Signin failed');
        }
      },

      signup: async (email, password) => {
        try {
          const signupRes = await axios.post<ApiResult<AuthResponse>>(
            API_ENDPOINTS.AUTH.SIGNUP,
            { email, password },
          );

          if (!signupRes.data.success) {
            throw new Error(signupRes.data.errors[0]?.error || 'Signup failed');
          }

          const auth = signupRes.data.result;
          get().setAuth(auth);
          return auth;
        } catch (error) {
          rethrowApiError(error, 'Signup failed');
        }
      },

      signinWithGoogle: async (idToken) => {
        try {
          const res = await axios.post<ApiResult<AuthResponse>>(
            API_ENDPOINTS.AUTH.GOOGLE,
            { idToken },
          );

          if (!res.data.success) {
            throw new Error(
              res.data.errors[0]?.error || 'Google sign-in failed',
            );
          }

          const auth = res.data.result;
          get().setAuth(auth);
          return auth;
        } catch (error) {
          rethrowApiError(error, 'Google sign-in failed');
        }
      },

      confirmEmail: async (userId, token) => {
        try {
          const res = await axios.post<ApiResult<AuthResponse>>(
            API_ENDPOINTS.AUTH.CONFIRM_EMAIL,
            { userId, token },
          );

          if (!res.data.success) {
            throw new Error(
              res.data.errors[0]?.error || 'Email confirmation failed',
            );
          }

          get().setAuth(res.data.result);
          return res.data.result;
        } catch (error) {
          rethrowApiError(error, 'Email confirmation failed');
        }
      },

      logout: async () => {
        try {
          await axios.post(API_ENDPOINTS.AUTH.LOGOUT);
        } finally {
          set({
            userId: null,
            accessToken: null,
            refreshToken: null,
            requiredAction: null,
          });
        }
      },

      requestPasswordReset: async (email) => {
        try {
          const res = await axios.post<ApiResult<any>>(
            API_ENDPOINTS.AUTH.REQUEST_PASSWORD_RESET,
            { email },
          );

          if (!res.data.success) {
            const error = res.data.errors[0];
            throw new Error(error?.error || 'Password reset request failed');
          }
        } catch (error) {
          rethrowApiError(error, 'Password reset request failed');
        }
      },

      confirmPasswordReset: async (encodedEmail, encodedToken, newPassword) => {
        try {
          const res = await axios.post<ApiResult<any>>(
            API_ENDPOINTS.AUTH.CONFIRM_PASSWORD_RESET,
            {
              encodedEmail,
              encodedToken,
              newPassword,
            },
          );

          if (!res.data.success) {
            const error = res.data.errors[0];
            throw new Error(error?.error || 'Password reset failed');
          }
        } catch (error) {
          rethrowApiError(error, 'Password reset failed');
        }
      },
    }),
    {
      name: 'auth-store',
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
      partialize: (state) => ({
        userId: state.userId,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        requiredAction: state.requiredAction,
      }),
    },
  ),
);

// --- Axios Interceptors ---

axios.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState();
  if (accessToken && config.headers) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// Flag to prevent infinite loop
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: any) => void;
}> = [];

const handleAuthFailure = () => {
  const { logout } = useAuthStore.getState();
  logout();
  // We are outside of a React component, so we cannot use here the useRouter hook.
  if (typeof window !== 'undefined') {
    window.location.href = PAGE_ROUTES.LOGIN();
  }
};

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token!);
    }
  });

  failedQueue = [];
};

axios.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Prevent infinite loops for auth endpoints
    if (
      originalRequest.url.includes(API_ENDPOINTS.AUTH.SIGNIN) ||
      originalRequest.url.includes(API_ENDPOINTS.AUTH.SIGNUP) ||
      originalRequest.url.includes(API_ENDPOINTS.AUTH.RENEW_TOKENS)
    ) {
      return Promise.reject(error);
    }

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise(function (resolve, reject) {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers['Authorization'] = 'Bearer ' + token;
            return axios(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const { refreshToken, setAuth, logout } = useAuthStore.getState();

      if (!refreshToken) {
        handleAuthFailure();
        return Promise.reject(error);
      }

      try {
        const response = await axios.post<ApiResult<AuthResponse>>(
          API_ENDPOINTS.AUTH.RENEW_TOKENS,
          { refreshToken },
        );

        if (response.data.success && response.data.result.accessToken) {
          const newAuth = response.data.result;
          setAuth(newAuth);

          originalRequest.headers['Authorization'] =
            'Bearer ' + newAuth.accessToken;

          processQueue(null, newAuth.accessToken);
          return axios(originalRequest);
        } else {
          throw new Error('Failed to renew token');
        }
      } catch (err) {
        processQueue(err, null);
        console.warn('⛔️ Token renewal failed! Logging out...');
        handleAuthFailure();
        return Promise.reject(err);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  },
);
