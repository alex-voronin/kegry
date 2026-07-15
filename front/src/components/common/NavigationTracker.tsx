'use client';

import { useEffect } from 'react';
import { usePathname } from 'next/navigation';
import { STORAGE_CURRENT_PATH, STORAGE_PREVIOUS_PATH } from '@/hooks/use-safe-back';

export function NavigationTracker() {
  const pathname = usePathname();

  useEffect(() => {
    if (typeof window === 'undefined') return;

    const storage = sessionStorage;
    const current = storage.getItem(STORAGE_CURRENT_PATH);

    if (current && current !== pathname) {
      storage.setItem(STORAGE_PREVIOUS_PATH, current);
    }

    storage.setItem(STORAGE_CURRENT_PATH, pathname);
  }, [pathname]);

  return null;
}
