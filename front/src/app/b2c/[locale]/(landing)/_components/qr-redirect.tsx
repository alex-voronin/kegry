'use client';

import { useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { useRouter } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import { storeQrCode } from '@/lib/qr-utils';
import { toast } from 'react-toastify';
import { useTranslations } from 'next-intl';

export function QrRedirect() {
  const searchParams = useSearchParams();
  const v = searchParams.get('v');
  const accessToken = useAuthStore((state) => state.accessToken);
  const hasHydrated = useAuthStore((state) => state.hasHydrated);
  const router = useRouter();
  const t = useTranslations('common');

  useEffect(() => {
    if (v) {
      storeQrCode(v);

      if (hasHydrated && accessToken) {
        router.replace(`${PAGE_ROUTES.FEED}`);
      } else if (hasHydrated && !accessToken) {
        toast.success(t('toast.qrScanned'), { className: 'Toastify__toast--qr-scanned' });
        setTimeout(() => {
          const element = document.getElementById('mobile-loyalty');
          if (element) {
            element.scrollIntoView({ behavior: 'smooth' });
          }
        }, 100);
      }
    }
  }, [v, hasHydrated, accessToken, router, t]);

  return null;
}
