'use client';

import { useEffect, useRef, useCallback } from 'react';
import axios from 'axios';
import { API_ENDPOINTS } from '@/lib/routes';
import { useAuthStore } from '@/lib/auth/use-auth-store';

const LOCATION_UPDATE_INTERVAL = 5 * 60 * 1000; // 5 minutes
const ENABLE_HIGH_ACCURACY = false;

interface GeolocationCoordinates {
  latitude: number;
  longitude: number;
}

async function sendLocationToServer(coords: GeolocationCoordinates) {
  try {
    await axios.post(API_ENDPOINTS.LOCATION.UPDATE, {
      latitude: coords.latitude,
      longitude: coords.longitude,
    });
  } catch (error) {
    console.error('Failed to send location:', error);
  }
}

export function useBackgroundLocation() {
  const accessToken = useAuthStore((state) => state.accessToken);
  const isAuthenticated = !!accessToken;
  const watchIdRef = useRef<number | null>(null);
  const lastSentRef = useRef<number>(0);
  const lastCoordsRef = useRef<GeolocationCoordinates | null>(null);

  const handlePosition = useCallback((position: GeolocationPosition) => {
    const coords = {
      latitude: position.coords.latitude,
      longitude: position.coords.longitude,
    };
    lastCoordsRef.current = coords;

    const now = Date.now();
    if (now - lastSentRef.current >= LOCATION_UPDATE_INTERVAL) {
      lastSentRef.current = now;
      sendLocationToServer(coords);
    }
  }, []);

  const handleError = useCallback((error: GeolocationPositionError) => {
    console.warn('Geolocation error:', error.message);
  }, []);

  useEffect(() => {
    if (!isAuthenticated) {
      if (watchIdRef.current !== null) {
        navigator.geolocation.clearWatch(watchIdRef.current);
        watchIdRef.current = null;
      }
      return;
    }

    if (!navigator.geolocation) {
      console.warn('Geolocation is not supported');
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const coords = {
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        };
        lastCoordsRef.current = coords;
        lastSentRef.current = Date.now();
        sendLocationToServer(coords);
      },
      handleError,
      {
        enableHighAccuracy: ENABLE_HIGH_ACCURACY,
        timeout: 10000,
        maximumAge: 0,
      },
    );

    watchIdRef.current = navigator.geolocation.watchPosition(
      handlePosition,
      handleError,
      {
        enableHighAccuracy: ENABLE_HIGH_ACCURACY,
        timeout: 10000,
        maximumAge: LOCATION_UPDATE_INTERVAL,
      },
    );

    const intervalId = setInterval(() => {
      if (
        lastCoordsRef.current &&
        Date.now() - lastSentRef.current >= LOCATION_UPDATE_INTERVAL
      ) {
        lastSentRef.current = Date.now();
        sendLocationToServer(lastCoordsRef.current);
      }
    }, LOCATION_UPDATE_INTERVAL);

    return () => {
      if (watchIdRef.current !== null) {
        navigator.geolocation.clearWatch(watchIdRef.current);
        watchIdRef.current = null;
      }
      clearInterval(intervalId);
    };
  }, [isAuthenticated, handlePosition, handleError]);
}
