import posthog from 'posthog-js';
import { useAuthStore } from './lib/auth/use-auth-store';

if (typeof window === 'undefined') {
  throw new Error('This file should only run on client');
}

posthog.init(process.env.NEXT_PUBLIC_POSTHOG_KEY as string, {
  api_host: process.env.NEXT_PUBLIC_POSTHOG_HOST,
  defaults: '2025-11-30',
  debug: window.location.hostname === 'localhost',
  disable_session_recording: window.location.hostname === 'localhost',
  loaded: (ph) => {
    const env = window.location.hostname === 'localhost' ? 'dev' : 'prod';
    ph.register({ environment: env });
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
