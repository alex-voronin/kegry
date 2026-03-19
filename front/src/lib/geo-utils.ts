/**
 * Parses latitude and longitude from a Google Maps URL.
 * Supports formats like:
 * - https://www.google.com/maps/place/Some+Place/@48.137154,11.576124,17z/...
 * - https://www.google.com/maps/@48.137154,11.576124,17z
 * - https://www.google.com/maps?q=48.137154,11.576124
 * - https://maps.google.com/?q=48.137154,11.576124
 */
export function parseGoogleMapsUrl(url: string): { latitude: number; longitude: number } | null {
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
