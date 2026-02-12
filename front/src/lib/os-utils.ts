export type OS = 'ios' | 'android' | 'other';

export function getOS(): OS {
  if (typeof window === 'undefined') return 'other';

  const ua = navigator.userAgent || navigator.vendor || (window as any).opera;

  // iOS detection
  if (/iPad|iPhone|iPod/.test(ua) && !(window as any).MSStream) {
    return 'ios';
  }

  // Android detection
  if (/android/i.test(ua)) {
    return 'android';
  }

  return 'other';
}
