'use client';

import { useEffect, useRef, useCallback } from 'react';
import { getLowAccuracyLocation } from './geo-utils';

import { useAuthStore } from '@/lib/auth/use-auth-store';

import { checkPermissionStatus } from './permissions';

const WARMUP_INTERVAL = 3 * 60 * 1000; // 3 minutes

/**
 * Hook to "warm up" the location cache by requesting a low-accuracy position.
 * Triggered on window focus or visibility change, but throttled to occur
 * at most once every 3 minutes.
 */
export function useLocationWarmer() {
  const accessToken = useAuthStore((state) => state.accessToken);
  const isAuthenticated = !!accessToken;

  const lastWarmupRef = useRef<number>(0);
  const isWarmingUpRef = useRef<boolean>(false);

  const warmup = useCallback(async () => {
    if (!isAuthenticated || isWarmingUpRef.current) return;

    const now = Date.now();
    if (now - lastWarmupRef.current < WARMUP_INTERVAL) return;

    const { locationAllowed } = await checkPermissionStatus();
    if (!locationAllowed) return;

    isWarmingUpRef.current = true;
    lastWarmupRef.current = now;
    try {
      // getLowAccuracyLocation updates the local cache internally
      await getLowAccuracyLocation();
    } catch (e) {
      // Warmup failed, we'll wait for the next trigger
      console.warn('Geolocation warmup failed:', e);
    } finally {
      isWarmingUpRef.current = false;
    }
  }, [isAuthenticated]);

  useEffect(() => {
    if (!isAuthenticated) return;

    // Initial warmup on mount
    warmup();

    const handleFocus = () => warmup();
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        warmup();
      }
    };

    window.addEventListener('focus', handleFocus);
    window.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      window.removeEventListener('focus', handleFocus);
      window.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [warmup, isAuthenticated]);
}
