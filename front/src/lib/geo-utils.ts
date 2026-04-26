/**
 * Shared interface for geographic coordinates.
 */
export interface LocationCoords {
  latitude: number;
  longitude: number;
}

const LAST_LOCATION_CACHE_KEY = 'last_known_location';
const LOW_ACCURACY_TIMEOUT = 5000;
const HIGH_ACCURACY_TIMEOUT = 10000;
const CACHE_TTL_MS = 24 * 60 * 60 * 1000; // 24 hours

interface CachedLocation extends LocationCoords {
  timestamp: number;
}

// --- Internal Helpers ---

function getCurrentPositionAsync(
  options: PositionOptions,
): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) => {
    if (typeof window === 'undefined' || !navigator.geolocation) {
      return reject(new Error('Geolocation not supported'));
    }
    navigator.geolocation.getCurrentPosition(resolve, reject, options);
  });
}

export function saveLocationToCache(latitude: number, longitude: number) {
  if (typeof window !== 'undefined') {
    const data: CachedLocation = {
      latitude,
      longitude,
      timestamp: Date.now(),
    };
    localStorage.setItem(LAST_LOCATION_CACHE_KEY, JSON.stringify(data));
  }
}

function getCachedLocation(maxAgeMs?: number): LocationCoords | null {
  if (typeof window === 'undefined') return null;
  const cached = localStorage.getItem(LAST_LOCATION_CACHE_KEY);
  if (!cached) return null;

  try {
    const data = JSON.parse(cached) as CachedLocation;
    const now = Date.now();
    const age = now - data.timestamp;

    // 1. Check absolute expiration accurately
    if (age > CACHE_TTL_MS) {
      localStorage.removeItem(LAST_LOCATION_CACHE_KEY);
      return null;
    }

    // 2. Check custom "freshness" expiration if requested
    if (maxAgeMs !== undefined && age > maxAgeMs) {
      return null;
    }

    return { latitude: data.latitude, longitude: data.longitude };
  } catch (e) {
    console.error('Failed to parse cached location', e);
    return null;
  }
}

// --- Public API ---

/**
 * Parses latitude and longitude from a Google Maps URL.
 * Supports formats like:
 * - https://www.google.com/maps/place/Some+Place/@48.137154,11.576124,17z/...
 * - https://www.google.com/maps/@48.137154,11.576124,17z
 * - https://www.google.com/maps?q=48.137154,11.576124
 * - https://maps.google.com/?q=48.137154,11.576124
 */
export function parseGoogleMapsUrl(url: string): LocationCoords | null {
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
 * Gets a low accuracy geolocation with a fallback to localStorage cache.
 */
export async function getLowAccuracyLocation(): Promise<LocationCoords> {
  const freshLocation = getCachedLocation(300000); // 5 minutes fresh
  if (freshLocation) return freshLocation;

  try {
    const position = await getCurrentPositionAsync({
      enableHighAccuracy: false,
      maximumAge: 600000,
      timeout: LOW_ACCURACY_TIMEOUT,
    });

    const coords = {
      latitude: position.coords.latitude,
      longitude: position.coords.longitude,
    };

    saveLocationToCache(coords.latitude, coords.longitude);
    return coords;
  } catch (error) {
    const cached = getCachedLocation();
    if (cached) return cached;
    throw error;
  }
}

/**
 * Gets a high accuracy geolocation by watching position for up to 10 seconds.
 * Resolves early if accuracy <= expectedAccuracy. Falls back to best position or standard getCurrentPosition.
 */
export async function getHighAccuracyLocation(
  expectedAccuracy: number = 50,
): Promise<LocationCoords> {
  // 1. Try fast path first
  try {
    const fastPosition = await getCurrentPositionAsync({
      enableHighAccuracy: false,
      maximumAge: 30000,
      timeout: 2500,
    });

    if (fastPosition.coords.accuracy <= expectedAccuracy) {
      return {
        latitude: fastPosition.coords.latitude,
        longitude: fastPosition.coords.longitude,
      };
    }
  } catch (error) {
    // Ignore errors from fast path and fallback to slow path
  }

  // 2. Slow path with watchPosition
  return new Promise((resolve, reject) => {
    let resolved = false;
    let bestPosition: GeolocationPosition | null = null;

    const watchId = navigator.geolocation.watchPosition(
      (position) => {
        const { accuracy } = position.coords;

        if (!bestPosition || accuracy < bestPosition.coords.accuracy) {
          bestPosition = position;
        }

        if (accuracy <= expectedAccuracy && !resolved) {
          resolved = true;
          navigator.geolocation.clearWatch(watchId);
          saveLocationToCache(
            position.coords.latitude,
            position.coords.longitude,
          );
          resolve({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
          });
        }
      },
      (error) => {
        if (!resolved) {
          resolved = true;
          navigator.geolocation.clearWatch(watchId);
          reject(error);
        }
      },
      {
        enableHighAccuracy: true,
        maximumAge: 10000,
        timeout: HIGH_ACCURACY_TIMEOUT,
      },
    );

    // Safety timeout to resolve with best available position
    setTimeout(async () => {
      if (resolved) return;

      resolved = true;
      navigator.geolocation.clearWatch(watchId);

      if (bestPosition) {
        saveLocationToCache(
          bestPosition.coords.latitude,
          bestPosition.coords.longitude,
        );
        resolve({
          latitude: bestPosition.coords.latitude,
          longitude: bestPosition.coords.longitude,
        });
      } else {
        try {
          const finalPos = await getCurrentPositionAsync({
            enableHighAccuracy: false,
            timeout: 3000,
          });
          saveLocationToCache(
            finalPos.coords.latitude,
            finalPos.coords.longitude,
          );
          resolve({
            latitude: finalPos.coords.latitude,
            longitude: finalPos.coords.longitude,
          });
        } catch (err) {
          reject(err);
        }
      }
    }, HIGH_ACCURACY_TIMEOUT);
  });
}
