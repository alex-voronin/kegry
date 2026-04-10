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
        // if we can't go back then all navigation branch must be unrolled up to the root
        // So, delete all keys related to navigation to force fallback branch on the next
        // safeBack() call
        sessionStorage.removeItem('currentPath');
        sessionStorage.removeItem('previousPath');
        router.replace(fallbackUrl);
      } else {
        window.close();
      }
    }
  };

  return { safeBack };
};
