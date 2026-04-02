export function isOurDomain(url: URL): boolean {
  const { hostname } = window.location;
  const parts = hostname.split('.');
  const ownRoot = parts.slice(-2).join('.');
  const scannedRoot = url.hostname.split('.').slice(-2).join('.');
  return ownRoot === scannedRoot;
}

export function buildQrUrl(qrCode: string = ''): string {
  if (typeof window === 'undefined' || !qrCode) return '';
  const { protocol, hostname, port } = window.location;
  let rootDomain = hostname;
  const parts = hostname.split('.');

  // Strip subdomain
  if (parts.length > 2 || (parts.length === 2 && parts[1] === 'localhost')) {
    parts.shift();
    rootDomain = parts.join('.');
  }

  const portStr = port ? `:${port}` : '';
  return `${protocol}//${rootDomain}${portStr}?v=${qrCode}`;
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
