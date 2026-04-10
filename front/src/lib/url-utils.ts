export function getInternalPath(url: string): string | null {
  if (typeof window === 'undefined') return null;
  try {
    const parsedUrl = new URL(url.startsWith('http') ? url : `https://${url}`);
    if (parsedUrl.hostname === window.location.hostname) {
      return parsedUrl.pathname + parsedUrl.search + parsedUrl.hash;
    }
  } catch {
    // Ignore malformed URLs
  }
  return null;
}

export function parseInstagramHandle(
  url: string | undefined | null,
): string | null {
  if (!url) return null;

  try {
    // Add protocol if missing so the URL constructor doesn't throw an error
    const validUrl = url.startsWith('http') ? url : `https://${url}`;
    const { pathname } = new URL(validUrl);

    // Extract path segments and filter out empty ones
    const segments = pathname.split('/').filter(Boolean);
    const lastSegment = segments.at(-1);

    // If there are no segments (e.g., only the domain was provided), return null
    return lastSegment || null;
  } catch {
    // If it's not a valid URL (possibly just a username), return the input as is
    return url;
  }
}
