/**
 * Parses latitude and longitude from a Google Maps URL.
 * Supports formats like:
 * - https://www.google.com/maps/place/Some+Place/@48.137154,11.576124,17z/...
 * - https://www.google.com/maps/@48.137154,11.576124,17z
 * - https://www.google.com/maps?q=48.137154,11.576124
 * - https://maps.google.com/?q=48.137154,11.576124
 */
export function parseGoogleMapsUrl(
  url: string,
): { latitude: number; longitude: number } | null {
  if (!url) return null;

  // Try @lat,lng format
  const atMatch = url.match(/@(-?\d+\.\d+),(-?\d+\.\d+)/);
  if (atMatch) {
    return {
      latitude: parseFloat(atMatch[1]),
      longitude: parseFloat(atMatch[2]),
    };
  }

  // Try q=lat,lng format
  const qMatch = url.match(/[?&]q=(-?\d+\.\d+),(-?\d+\.\d+)/);
  if (qMatch) {
    return {
      latitude: parseFloat(qMatch[1]),
      longitude: parseFloat(qMatch[2]),
    };
  }

  return null;
}

/**
 * Gets the current geolocation coordinates of the user.
 * Wraps navigator.geolocation.getCurrentPosition in a Promise.
 */
export async function getLowAccuracyLocation(): Promise<{
  latitude: number;
  longitude: number;
}> {
  const position = await new Promise<GeolocationPosition>((resolve, reject) => {
    const options = {
      enableHighAccuracy: false,
      maximumAge: 300000,
      timeout: 5000,
    };
    navigator.geolocation.getCurrentPosition(resolve, reject, options);
  });

  return {
    latitude: position.coords.latitude,
    longitude: position.coords.longitude,
  };
}

/**
 * Gets a high accuracy geolocation by watching position for up to 7 seconds.
 * Resolves early if accuracy <= expectedAccuracy. Falls back to best position or standard getCurrentPosition.
 */
export async function getHighAccuracyLocation(expectedAccuracy: number = 50): Promise<GeolocationPosition> {
  const TIMEOUT_MS = 7000;

  return new Promise((resolve, reject) => {
    let resolved = false;
    let bestPosition: GeolocationPosition | null = null;

    const cleanup = (id: number) => {
      navigator.geolocation.clearWatch(id);
    };

    const watchId = navigator.geolocation.watchPosition(
      (position) => {
        const { accuracy } = position.coords;

        // save the best point
        if (!bestPosition || accuracy < bestPosition.coords.accuracy) {
          bestPosition = position;
        }

        // if accuracy is excellent - return immediately
        if (accuracy <= expectedAccuracy && !resolved) {
          resolved = true;
          cleanup(watchId);
          resolve(position);
        }
      },
      (error) => {
        if (!resolved) {
          resolved = true;
          cleanup(watchId);
          reject(error);
        }
      },
      {
        enableHighAccuracy: true,
        maximumAge: 10000,
        timeout: TIMEOUT_MS,
      },
    );

    setTimeout(() => {
      if (resolved) return;

      resolved = true;
      cleanup(watchId);

      if (bestPosition) {
        resolve(bestPosition);
      } else {
        navigator.geolocation.getCurrentPosition(resolve, reject, {
          enableHighAccuracy: false,
        });
      }
    }, TIMEOUT_MS);
  });
}
