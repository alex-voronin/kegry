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
