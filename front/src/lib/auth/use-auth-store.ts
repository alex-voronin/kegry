'use client';

import { create, resetAllStores } from '@/lib/zustand-reset';
import { persist } from 'zustand/middleware';
import authClient from '../auth-client';
import { API_ENDPOINTS, PAGE_ROUTES } from '../routes';
import { rethrowApiError, ApiRequestError } from '../api-error';
import { reRegisterServiceWorker } from '../service-worker';

// Note: Auth endpoints use raw axios to avoid token interceptor issues during login/signup.
// For other API calls, use apiClient from '../api-client' for automatic token handling and retries.

// --- Types ---

export interface ApiResult<T> {
  success: boolean;
  result: T;
}

export enum UserType {
  B2B = 'B2B',
  B2C = 'B2C',
}

export enum RequiredAction {
  None = 'None',
  EmailVerification = 'EmailVerification',
  OnboardingCompletion = 'OnboardingCompletion',
  // it's a fake value that exists only on frontend
  // we use it to open a how-it-works page after pwa installation
  // if it's installed on how-it-works page
  HowItWorksCompletion = 'HowItWorksCompletion',
}

export interface AuthResponse {
  userId: string;
  userType: UserType;
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

export const hasCompletedOnboarding = (state: AuthState): boolean =>
  !!state.accessToken && state.requiredAction === RequiredAction.None;

interface AuthState {
  userId: string | null;
  accessToken: string | null;
  refreshToken: string | null;
  requiredAction: RequiredAction | null;
  userType: UserType | null;
  hasHydrated: boolean;

  setHasHydrated: (hydrated: boolean) => void;

  signin: (
    email: string,
    password: string,
    allowedUserTypes: UserType[],
  ) => Promise<AuthResponse>;
  signup: (
    email: string,
    password: string,
    userType: UserType,
  ) => Promise<AuthResponse>;
  signinWithGoogle: (
    idToken: string,
    userType: UserType,
    allowedUserTypes: UserType[],
  ) => Promise<AuthResponse>;
  confirmEmail: (userId: string, token: string) => Promise<AuthResponse>;
  resendConfirmationEmail: (userId: string) => Promise<void>;
  logout: (deviceId?: string) => Promise<void>;
  requestPasswordReset: (email: string) => Promise<void>;
  confirmPasswordReset: (
    encodedEmail: string,
    encodedToken: string,
    newPassword: string,
  ) => Promise<void>;
  deleteAccount: () => Promise<void>;

  // Internal/Helper action to update tokens
  setAuth: (auth: AuthResponse) => void;
  setRequiredAction: (action: RequiredAction) => void;
}

const RESEND_COOLDOWN_MS = 30_000;
let lastResendTimestamp = 0;

// --- Store ---

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      userId: null,
      accessToken: null,
      refreshToken: null,
      requiredAction: null,
      userType: null,
      hasHydrated: false,

      setHasHydrated: (hydrated: boolean) => {
        set({ hasHydrated: hydrated });
      },

      setAuth: (auth: AuthResponse) => {
        set({
          userId: auth.userId,
          userType: auth.userType,
          accessToken: auth.accessToken,
          refreshToken: auth.refreshToken,
          requiredAction: auth.requiredAction,
        });
      },

      setRequiredAction: (action: RequiredAction) => {
        set({ requiredAction: action });
      },

      signin: async (email, password, allowedUserTypes) => {
        try {
          const signinRes = await authClient.post<ApiResult<SigninResponse>>(
            API_ENDPOINTS.AUTH.SIGNIN,
            { email, password },
          );

          const { isSignupRequired, auth } = signinRes.data.result;

          if (isSignupRequired) {
            throw new ApiRequestError(
              'Account not found. Please sign up first.',
              'AccountNotFound',
            );
          }

          if (!auth) {
            throw new Error('Unexpected auth state');
          }

          if (allowedUserTypes.includes(auth.userType)) {
            await reRegisterServiceWorker().catch((err) =>
              console.error('[auth] SW re-registration failed:', err),
            );
            get().setAuth(auth);
          }

          return auth;
        } catch (error) {
          rethrowApiError(error, 'Signin failed');
        }
      },

      signup: async (email, password, userType) => {
        try {
          const signupRes = await authClient.post<ApiResult<AuthResponse>>(
            API_ENDPOINTS.AUTH.SIGNUP,
            { email, password, userType },
          );

          const auth = signupRes.data.result;
          await reRegisterServiceWorker().catch((err) =>
            console.error('[auth] SW re-registration failed:', err),
          );
          get().setAuth(auth);
          return auth;
        } catch (error) {
          rethrowApiError(error, 'Signup failed');
        }
      },

      signinWithGoogle: async (idToken, userType, allowedUserTypes) => {
        try {
          const res = await authClient.post<ApiResult<AuthResponse>>(
            API_ENDPOINTS.AUTH.GOOGLE,
            { idToken, userType },
          );

          const auth = res.data.result;

          if (allowedUserTypes.includes(auth.userType)) {
            await reRegisterServiceWorker().catch((err) =>
              console.error('[auth] SW re-registration failed:', err),
            );
            get().setAuth(auth);
          }

          return auth;
        } catch (error) {
          rethrowApiError(error, 'Google sign-in failed');
        }
      },

      confirmEmail: async (userId, token) => {
        try {
          const res = await authClient.post<ApiResult<AuthResponse>>(
            API_ENDPOINTS.AUTH.CONFIRM_EMAIL,
            { userId, token },
          );

          get().setAuth(res.data.result);
          return res.data.result;
        } catch (error) {
          rethrowApiError(error, 'Email verification failed');
        }
      },

      resendConfirmationEmail: async (userId) => {
        const now = Date.now();
        if (now - lastResendTimestamp < RESEND_COOLDOWN_MS) {
          throw new ApiRequestError('Cooldown active', 'CooldownActive');
        }

        try {
          await authClient.post<ApiResult<unknown>>(
            API_ENDPOINTS.AUTH.RESEND_CONFIRMATION_EMAIL,
            { userId },
          );

          lastResendTimestamp = now;
        } catch (error) {
          rethrowApiError(error, 'Failed to resend verification email');
        }
      },

      logout: async (deviceId?: string) => {
        try {
          await authClient.post(
            API_ENDPOINTS.AUTH.LOGOUT,
            deviceId ? { deviceId } : {},
          );
        } finally {
          if (typeof window === 'undefined') return;
          resetAllStores();
          localStorage.clear();
          sessionStorage.clear();
        }
      },

      requestPasswordReset: async (email) => {
        try {
          await authClient.post<ApiResult<unknown>>(
            API_ENDPOINTS.AUTH.REQUEST_PASSWORD_RESET,
            { email },
          );
        } catch (error) {
          rethrowApiError(error, 'Password reset request failed');
        }
      },

      confirmPasswordReset: async (encodedEmail, encodedToken, newPassword) => {
        try {
          await authClient.post<ApiResult<unknown>>(
            API_ENDPOINTS.AUTH.CONFIRM_PASSWORD_RESET,
            {
              encodedEmail,
              encodedToken,
              newPassword,
            },
          );
        } catch (error) {
          rethrowApiError(error, 'Password reset failed');
        }
      },

      deleteAccount: async () => {
        try {
          await authClient.delete(API_ENDPOINTS.AUTH.DELETE_ACCOUNT);
        } catch (error) {
          rethrowApiError(error, 'Account deletion failed');
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
        userType: state.userType,
      }),
    },
  ),
);

// --- Axios Interceptors ---

authClient.interceptors.request.use((config) => {
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
  reject: (error: unknown) => void;
}> = [];

const handleAuthFailure = () => {
  const { logout } = useAuthStore.getState();
  logout();
  // We are outside of a React component, so we cannot use here the useRouter hook.
  if (typeof window !== 'undefined') {
    window.location.href = PAGE_ROUTES.LOGIN();
  }
};

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token!);
    }
  });

  failedQueue = [];
};

authClient.interceptors.response.use(
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
            return authClient(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const { refreshToken, setAuth } = useAuthStore.getState();

      if (!refreshToken) {
        handleAuthFailure();
        return Promise.reject(error);
      }

      try {
        const response = await authClient.post<ApiResult<AuthResponse>>(
          API_ENDPOINTS.AUTH.RENEW_TOKENS,
          { refreshToken },
        );

        if (response.data.success && response.data.result.accessToken) {
          const newAuth = response.data.result;
          setAuth(newAuth);

          originalRequest.headers['Authorization'] =
            'Bearer ' + newAuth.accessToken;

          processQueue(null, newAuth.accessToken);
          return authClient(originalRequest);
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
