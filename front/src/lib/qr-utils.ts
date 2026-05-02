export function isOurDomain(url: URL): boolean {
  const { hostname } = window.location;
  const parts = hostname.split('.');
  const ownRoot = parts.slice(-2).join('.');
  const scannedRoot = url.hostname.split('.').slice(-2).join('.');
  return (
    ownRoot === scannedRoot ||
    scannedRoot === 'kegry.rs' ||
    scannedRoot === 'kegry.com'
  );
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

export async function downloadQrCode(url: string, style: 'default' | 'bw') {
  const { default: QRCodeStyling } = await import('qr-code-styling');
  const qrCode = extractQrCode(url) || 'check-in';
  const isBW = style === 'bw';

  const qr = new QRCodeStyling({
    width: 590,
    height: 590,
    type: 'svg',
    data: url,
    image: '/assets/b2b/qr-logo.webp',
    backgroundOptions: {
      color: isBW ? '#ffffff' : '#333333',
      round: isBW ? 0 : 0.1,
    },
    dotsOptions: {
      color: isBW ? '#000000' : 'white',
      type: 'square',
    },
    cornersSquareOptions: {
      type: 'extra-rounded',
      color: isBW ? '#000000' : '#ffab10',
    },
    cornersDotOptions: {
      type: 'dot',
      color: isBW ? '#000000' : 'white',
    },
    qrOptions: {
      errorCorrectionLevel: 'Q',
    },
    imageOptions: {
      imageSize: 0.45,
      margin: 2,
    },
  });

  qr.download({
    name: `qr_${qrCode}_${isBW ? 'high_contrast' : 'default'}`,
    extension: 'png',
  });
}

export function extractQrCode(
  rawValue: string,
  failOnDomainMismatch: boolean = true,
): string | null {
  try {
    const url = new URL(rawValue);
    if (failOnDomainMismatch && !isOurDomain(url)) {
      return null;
    }
    return url.searchParams.get('v');
  } catch {
    return null;
  }
}
