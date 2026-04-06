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
