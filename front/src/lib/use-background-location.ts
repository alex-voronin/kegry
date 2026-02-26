'use client';

import { useEffect, useRef } from 'react';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';

const LOCATION_UPDATE_INTERVAL = 60 * 60 * 1000; // 1 hour
const ENABLE_HIGH_ACCURACY = false;
const LOCATION_LAST_SENT_KEY = 'location_last_sent';

function getLastSentTimestamp(): number {
  if (typeof window === 'undefined') return 0;
  const stored = localStorage.getItem(LOCATION_LAST_SENT_KEY);
  return stored ? parseInt(stored, 10) : 0;
}

function setLastSentTimestamp(timestamp: number) {
  if (typeof window !== 'undefined') {
    localStorage.setItem(LOCATION_LAST_SENT_KEY, timestamp.toString());
  }
}

function isRateLimited(): boolean {
  const now = Date.now();
  const lastSent = getLastSentTimestamp();
  return now - lastSent < LOCATION_UPDATE_INTERVAL;
}

export type LocationResult = 'success' | 'denied' | 'error';

let activeRequest: Promise<LocationResult> | null = null;

/**
 * Requests location and sends to server if rate limit allows.
 * Returns a promise with the result status.
 */
export function SendLocationWithRateLimit(): Promise<LocationResult> {
  if (activeRequest) {
    return activeRequest.finally(() => {
      activeRequest = null;
    });
  }

  activeRequest = (async (): Promise<LocationResult> => {
    try {
      if (typeof window === 'undefined' || !navigator.geolocation) {
        return 'error';
      }

      // Check rate limit before requesting location
      if (isRateLimited()) {
        return 'success';
      }

      return await new Promise<LocationResult>((resolve) => {
        navigator.geolocation.getCurrentPosition(
          async (position) => {
            // Double-check rate limit (could have been updated by another tab)
            if (!isRateLimited()) {
              try {
                await apiClient.post(API_ENDPOINTS.LOCATION.UPDATE, {
                  latitude: position.coords.latitude,
                  longitude: position.coords.longitude,
                });
                setLastSentTimestamp(Date.now());
                resolve('success');
              } catch (error) {
                console.error('Failed to send location:', error);
                resolve('error');
              }
            } else {
              resolve('success');
            }
          },
          (error) => {
            console.warn('Location error:', error.message);
            // Error code 1 = PERMISSION_DENIED
            resolve(error.code === 1 ? 'denied' : 'error');
          },
          {
            enableHighAccuracy: ENABLE_HIGH_ACCURACY,
            timeout: 10000,
            maximumAge: 300000,
          },
        );
      });
    } finally {
      activeRequest = null;
    }
  })();

  return activeRequest;
}

export function useBackgroundLocation() {
  const accessToken = useAuthStore((state) => state.accessToken);
  const requiredAction = useAuthStore((state) => state.requiredAction);
  const isAuthenticated = !!accessToken;
  const hasCompletedOnboarding = requiredAction === RequiredAction.None;
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }

    // Only start location updates after user has completed onboarding
    if (!isAuthenticated || !hasCompletedOnboarding) return;

    SendLocationWithRateLimit();

    intervalRef.current = setInterval(() => {
      try {
        SendLocationWithRateLimit();
      } catch (error) {
        console.error('Failed to send location:', error);
      }
    }, LOCATION_UPDATE_INTERVAL);

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, [isAuthenticated, hasCompletedOnboarding]);
}
