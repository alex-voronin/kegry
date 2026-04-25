'use client';

import { useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { useRouter } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';

export function QrRedirect() {
  const searchParams = useSearchParams();
  const v = searchParams.get('v');
  const accessToken = useAuthStore((state) => state.accessToken);
  const hasHydrated = useAuthStore((state) => state.hasHydrated);
  const router = useRouter();

  useEffect(() => {
    if (v) {
      localStorage.setItem('qr_code_to_check_in', v);
    }
    if (v && hasHydrated && accessToken) {
      router.replace(`${PAGE_ROUTES.FEED}`);
    }
  }, [v, hasHydrated, accessToken, router]);

  return null;
}
