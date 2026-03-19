'use client';

import { useRouter } from 'next/navigation';

export const useSafeBack = () => {
  const router = useRouter();

  const safeBack = (fallbackUrl: string) => {
    // Check if there's history beyond the current page.
    // window.history.length > 1 works for pages reached via internal routing.
    // For a more robust check, we check the length.
    if (typeof window !== 'undefined' && window.history.length > 1) {
      router.back();
    } else {
      router.push(fallbackUrl);
    }
  };

  return { safeBack };
};
