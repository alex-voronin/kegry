'use client';

import { useEffect } from 'react';
import { usePathname } from 'next/navigation';

export function NavigationTracker() {
  const pathname = usePathname();

  useEffect(() => {
    if (typeof window === 'undefined') return;

    const storage = sessionStorage;
    const current = storage.getItem('currentPath');

    if (current && current !== pathname) {
      storage.setItem('previousPath', current);
    }

    storage.setItem('currentPath', pathname);
  }, [pathname]);

  return null;
}
