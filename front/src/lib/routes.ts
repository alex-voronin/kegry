export const PAGE_ROUTES = {
  HOME: '/',
  LOGIN: '/login',
  FORGOT_PASSWORD: '/forgot',
  VERIFY_EMAIL: '/verify-email',
  PASSWORD_RESET: '/password-reset',
  FEED: '/feed',
  FEED_FILTERS: '/feed/filters',
  ONBOARDING: '/onboarding',
  ONBOARDING_COVER: '/onboarding/cover',
  PROFILE: '/profile',
  EDIT_PROFILE: '/edit-profile',
  NOTIFICATIONS: '/notifications',
  CHAT: '/chat',
  SETTINGS: '/settings',
} as const;

export const API_ENDPOINTS = {
  AUTH: {
    SIGNIN: '/api/v1/auth/signin',
    SIGNUP: '/api/v1/auth/signup',
    CONFIRM_EMAIL: '/api/v1/auth/confirm-email',
    LOGOUT: '/api/v1/auth/logout',
    REQUEST_PASSWORD_RESET: '/api/v1/auth/request-password-reset',
    CONFIRM_PASSWORD_RESET: '/api/v1/auth/confirm-password-reset',
    RENEW_TOKENS: '/api/v1/auth/renew-tokens',
  },
  USER_PROFILE: {
    PROFILE: '/api/v1/user-profile',
    AVATAR_UPLOAD_URL: '/api/v1/user-profile/avatar/upload-url',
    COVER_UPLOAD_URL: '/api/v1/user-profile/cover/upload-url',
  },
  HOBBIES: {
    LIST: '/api/v1/hobby',
  },
  FEED: {
    STATUS: '/api/v1/feed/status',
    LIST: '/api/v1/feed',
    SKIP: '/api/v1/feed',
    IGNORE: '/api/v1/feed',
    INVITE: '/api/v1/feed',
    RATE: '/api/v1/feed',
  },
} as const;
