'use client';

import { useEffect } from 'react';
import { useSearchParams } from 'next/navigation';

const REFERRER_KEY = 'referrer_id';

export const getReferrerId = () => {
  if (typeof window !== 'undefined') {
    return localStorage.getItem(REFERRER_KEY);
  }
  return null;
};

export const clearReferrerId = () => {
  if (typeof window !== 'undefined') {
    localStorage.removeItem(REFERRER_KEY);
  }
};

export function ReferrerTracker() {
  const searchParams = useSearchParams();

  useEffect(() => {
    const referrer = searchParams.get('referrer');
    if (referrer) {
      localStorage.setItem(REFERRER_KEY, referrer);
    }
  }, [searchParams]);

  return null;
}
