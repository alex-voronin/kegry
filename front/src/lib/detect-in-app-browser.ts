export function detectInAppBrowser(): boolean {
  if (typeof navigator === 'undefined') return false;
  return /instagram|fbav|fban|fbios|telegram|tiktok|twitter|linkedin|threads/i.test(
    navigator.userAgent,
  );
}
