'use client';

import { create } from 'zustand';
import axios from 'axios';
import { persist } from 'zustand/middleware';
import Router from 'next/router';

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

interface AuthState {
  userId: string | null;
  accessToken: string | null;
  refreshToken: string | null;
  requiredAction: RequiredAction | null;
  hasHydrated: boolean;

  setHasHydrated: (hydrated: boolean) => void;

  signin: (email: string, password: string) => Promise<AuthResponse>;
  confirmEmail: (userId: string, token: string) => Promise<AuthResponse>;
  logout: () => Promise<void>;

  // Internal/Helper action to update tokens
  setAuth: (auth: AuthResponse) => void;
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

      signin: async (email, password) => {
        try {
          // 1. Try to sign in
          const signinRes = await axios.post<ApiResult<SigninResponse>>(
            '/api/v1/auth/signin',
            { email, password },
          );

          if (!signinRes.data.success) {
            throw new Error(signinRes.data.errors[0]?.error || 'Signin failed'); // TODO: translate
          }

          const { isSignupRequired, auth } = signinRes.data.result;

          if (!isSignupRequired && auth) {
            get().setAuth(auth);
            return auth;
          }

          // 2. If signup is required, try to sign up
          if (isSignupRequired) {
            const signupRes = await axios.post<ApiResult<AuthResponse>>(
              '/api/v1/auth/signup',
              { email, password },
            );

            if (!signupRes.data.success) {
              throw new Error(
                signupRes.data.errors[0]?.error || 'Signup failed', // TODO: translate
              );
            }

            const signupAuth = signupRes.data.result;
            get().setAuth(signupAuth);
            return signupAuth;
          }

          throw new Error('Unexpected auth state');
        } catch (error) {
          if (axios.isAxiosError(error)) {
            const msg =
              error.response?.data?.errors?.[0]?.error || error.message;
            throw new Error(msg);
          }
          throw error;
        }
      },

      confirmEmail: async (userId, token) => {
        try {
          const res = await axios.post<ApiResult<AuthResponse>>(
            '/api/v1/auth/confirm-email',
            { userId, token },
          );

          if (!res.data.success) {
            throw new Error(
              res.data.errors[0]?.error || 'Email confirmation failed', // TODO: translate
            );
          }

          get().setAuth(res.data.result);
          return res.data.result;
        } catch (error) {
          if (axios.isAxiosError(error)) {
            const msg =
              error.response?.data?.errors?.[0]?.error || error.message;
            throw new Error(msg);
          }
          throw error;
        }
      },

      logout: async () => {
        try {
          await axios.post('/api/v1/auth/logout');
        } finally {
          set({
            userId: null,
            accessToken: null,
            refreshToken: null,
            requiredAction: null,
          });
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
  Router.push('/login');
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
      originalRequest.url.includes('/auth/signin') ||
      originalRequest.url.includes('/auth/signup') ||
      originalRequest.url.includes('/auth/renew-tokens')
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
          '/api/v1/auth/renew-tokens',
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
