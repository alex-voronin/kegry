'use client';

import { useCallback } from 'react';
import { useRouter } from '@/i18n/routing';
import authClient from '@/lib/auth-client';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { useAuthStore } from '@/lib/auth/use-auth-store';

const captureEvent = (event: string, props: Record<string, unknown>) => {
  if (typeof window !== 'undefined') {
    import('posthog-js')
      .then(({ default: posthog }) => posthog.capture(event, props))
      .catch(() => {});
  }
};

// Defensive guard: in rare edge cases the store's accessToken can go
// missing between refresh and feed requests (e.g. persist sync timing,
// store reversion). If refreshToken is still present, attempt an emergency
// recovery via renew-tokens instead of letting every feed request fail
// with 401 and redirecting to login.

export function useAuthRecovery(source: string) {
  const router = useRouter();

  const ensureAuthReady = useCallback(async (): Promise<boolean> => {
    const state = useAuthStore.getState();

    if (!state.accessToken && state.refreshToken) {
      console.warn(
        `[${source}] Inconsistent auth state before fetch — attempting emergency refresh`,
        {
          userId: state.userId,
          hasRefreshToken: !!state.refreshToken,
        },
      );
      captureEvent(`${source}_emergency_refresh`, {
        userId: state.userId,
      });

      try {
        const res = await authClient.post(API_ENDPOINTS.AUTH.RENEW_TOKENS, {
          refreshToken: state.refreshToken,
        });
        const { accessToken: recoveredAccess, refreshToken: recoveredRefresh } =
          res.data;

        if (!recoveredAccess || !recoveredRefresh) {
          router.push(PAGE_ROUTES.LOGIN());
          return false;
        }

        useAuthStore.setState({
          accessToken: recoveredAccess,
          refreshToken: recoveredRefresh,
        });

        return true;
      } catch {
        router.push(PAGE_ROUTES.LOGIN());
        return false;
      }
    }

    return true;
  }, [source, router]);

  return { ensureAuthReady };
}
