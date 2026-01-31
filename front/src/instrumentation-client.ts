import posthog from 'posthog-js';

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
