export type OS = 'ios' | 'android' | 'other';

export function getOS(): OS {
  // Return 'other' if running on server-side (no browser environment)
  if (typeof window === 'undefined') return 'other';

  const ua = navigator.userAgent.toLowerCase();
  const platform = navigator.platform;

  // Android detection - very reliable
  if (/android/.test(ua)) {
    return 'android';
  }

  // iOS detection by User-Agent (covers iPhone, iPod, and older iPads)
  const isIOSByUA = /ipad|iphone|ipod/.test(ua) && !(window as any).MSStream;

  // Additional check for modern iPads that pretend to be Mac (iPadOS 13+)
  const isMacWithTouch =
    platform === 'MacIntel' &&
    'maxTouchPoints' in navigator &&
    (navigator.maxTouchPoints as number) > 2; // iPads usually return 5

  // Check for touch event support (helps confirm it's a touch device)
  const hasTouchEvents = 'ontouchend' in document || 'ontouchstart' in window;

  // If any of the iOS checks pass, return 'ios'
  if (isIOSByUA || (isMacWithTouch && hasTouchEvents)) {
    return 'ios';
  }

  return 'other';
}
