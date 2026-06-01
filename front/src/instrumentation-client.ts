import posthog from 'posthog-js';
import { useAuthStore } from './lib/auth/use-auth-store';

if (typeof window === 'undefined') {
  throw new Error('This file should only run on client');
}

const hostname = window.location.hostname;
const isProd =
  hostname === 'kegry.rs' ||
  hostname === 'kegry.com' ||
  hostname.endsWith('.kegry.rs') ||
  hostname.endsWith('.kegry.com');

posthog.init(process.env.NEXT_PUBLIC_POSTHOG_KEY as string, {
  api_host: process.env.NEXT_PUBLIC_POSTHOG_HOST,
  defaults: '2025-11-30',
  debug: !isProd,
  disable_session_recording: !isProd,
  loaded: (ph) => {
    ph.register({ environment: isProd ? 'prod' : 'dev' });
  },
});

let currentUserId: string | null | undefined = useAuthStore.getState().userId;

if (currentUserId) {
  posthog.identify(currentUserId);
} else {
  posthog.reset();
}

useAuthStore.subscribe((state) => {
  const newUserId = state.userId;

  if (newUserId !== currentUserId) {
    if (newUserId) {
      posthog.identify(newUserId);
    } else {
      posthog.reset();
    }
    currentUserId = newUserId;
  }
});
