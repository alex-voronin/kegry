import { getCachedLocation } from './geo-utils';

export async function checkPermissionStatus(): Promise<{
  locationAllowed: boolean;
  notificationAllowed: boolean;
}> {
  let locationAllowed = false;
  let notificationAllowed = false;

  // Check Geolocation
  if (
    typeof navigator !== 'undefined' &&
    navigator.permissions &&
    navigator.permissions.query
  ) {
    try {
      // @ts-ignore
      const result = await navigator.permissions.query({ name: 'geolocation' });
      if (result.state === 'granted') {
        locationAllowed = true;
      }
    } catch (e) {
      console.warn('Geolocation permission query not supported', e);
    }
  }

  // Fallback for iOS/Browsers without permissions API support or "prompt" state
  // If we have a fresh cached location (< 3 hours), we can assume it was allowed recently
  if (!locationAllowed) {
    const freshLocation = getCachedLocation(3 * 60 * 60 * 1000);
    if (freshLocation) {
      locationAllowed = true;
    }
  }

  // Check Notifications
  if (typeof Notification !== 'undefined') {
    if (Notification.permission === 'granted') {
      notificationAllowed = true;
    }
  }

  return { locationAllowed, notificationAllowed };
}

export async function checkIfPermissionsDenied(): Promise<boolean> {
  const { locationAllowed, notificationAllowed } =
    await checkPermissionStatus();

  return !locationAllowed || !notificationAllowed;
}

export async function requestLocationPermission(): Promise<boolean> {
  return new Promise((resolve) => {
    if (typeof navigator !== 'undefined' && navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        () => resolve(true),
        () => resolve(false),
        {
          enableHighAccuracy: false,
          timeout: 4000,
          maximumAge: 60000,
        },
      );
    } else {
      resolve(false);
    }
  });
}

export async function requestNotificationPermission(): Promise<boolean> {
  if (typeof Notification !== 'undefined') {
    try {
      const permission = await Notification.requestPermission();
      return permission === 'granted';
    } catch (e) {
      console.warn('Failed to request notification permission', e);
      return false;
    }
  }
  return false;
}
