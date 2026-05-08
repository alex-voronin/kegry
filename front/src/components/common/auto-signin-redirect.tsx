'use client';

import { useEffect } from 'react';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { useRouter } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';

interface AutoSignInRedirectProps {
  isB2B?: boolean;
}

export function AutoSignInRedirect({ isB2B }: AutoSignInRedirectProps) {
  const { accessToken, hasHydrated, requiredAction } = useAuthStore();
  const router = useRouter();

  useEffect(() => {
    if (
      hasHydrated &&
      accessToken &&
      requiredAction !== RequiredAction.EmailVerification
    ) {
      const loginRoute = isB2B
        ? PAGE_ROUTES.B2B.LOGIN(true)
        : PAGE_ROUTES.LOGIN(true);
      router.replace(loginRoute);
    }
  }, [hasHydrated, accessToken, requiredAction, router, isB2B]);

  return null;
}
