export function isOurDomain(url: URL): boolean {
  const { hostname } = window.location;
  const ownRoot = hostname.split('.').slice(-2).join('.');
  const scannedRoot = url.hostname.split('.').slice(-2).join('.');
  return ownRoot === scannedRoot;
}

export function extractQrCode(rawValue: string): string | null {
  try {
    const url = new URL(rawValue);
    if (!isOurDomain(url)) return null;
    return url.searchParams.get('v');
  } catch {
    return null;
  }
}
