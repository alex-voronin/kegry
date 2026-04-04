'use client';

import { useRouter } from 'next/navigation';

export const useSafeBack = () => {
  const router = useRouter();

  const safeBack = (fallbackUrl?: string) => {
    const canGoBack =
      typeof window !== 'undefined' &&
      window.history.length > 1 &&
      sessionStorage.getItem('previousPath') !== null;

    if (canGoBack) {
      router.back();
    } else {
      if (fallbackUrl) {
        router.push(fallbackUrl);
      } else {
        window.close();
      }
    }
  };

  return { safeBack };
};
