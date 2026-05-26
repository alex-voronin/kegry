export const PAGE_ROUTES = {
  HOME: '/',
  LOGIN: (autoSignin?: boolean) =>
    autoSignin ? '/login?auto-signin=true' : '/login',
  REGISTER: '/register',
  FORGOT_PASSWORD: '/forgot',
  VERIFY_EMAIL: '/verify-email',
  PASSWORD_RESET: '/password-reset',
  FEED: '/feed',
  FEED_FILTERS: '/feed/filters',
  VENUES: '/venues',
  VENUES_MAP: '/venues/map',
  VENUE_DETAIL: (id: string) => `/venues/${id}`,
  VENUE_DISCOUNTS: (id: string) => `/venues/${id}/discounts`,
  ONBOARDING: '/onboarding',
  ONBOARDING_COVER: '/onboarding/cover',
  HOW_IT_WORKS: '/how-it-works',
  PROFILE: '/profile',
  USER_PROFILE: (userId: string) => `/profile/${userId}`,
  PROFILE_EDIT: '/profile/edit',
  PROFILE_COVER: '/profile/cover-image',
  DISCOUNTS_AND_PROMOTIONS: '/profile/discounts-and-promotions',
  EDIT_PROFILE: '/edit-profile',
  NOTIFICATIONS: '/notifications',
  CHAT: '/chat',
  SETTINGS: '/settings',
  FRIENDS_AND_FOES: '/friends-and-foes',
  RATE_USER: (userId: string) => `/rate-user/${userId}`,
  PERMISSIONS: '/permissions',
  LEGAL: {
    TERMS: '/terms-of-service',
    PRIVACY: '/privacy-policy',
    CONSENT: '/consent-to-communication',
    GUIDELINES: '/community-guidelines',
  },
  B2B: {
    HOME: '/',
    LOGIN: (autoSignin?: boolean) =>
      autoSignin ? '/login?auto-signin=true' : '/login',
    REGISTER: '/register',
    FORGOT_PASSWORD: '/forgot',
    VERIFY_EMAIL: '/verify-email',
    PASSWORD_RESET: '/password-reset',
    COMPANIES: '/companies',
    COMPANIES_CREATE: '/companies/create',
    COMPANIES_DETAIL: (id: string) => `/companies/${id}`,
    COMPANIES_EDIT: (id: string) => `/companies/${id}/edit`,
    COMPANY_LOYALTY_LEVEL: (companyId: string, levelId: number) =>
      `/companies/${companyId}/loyalty-levels/${levelId}`,
    VENUES: '/venues',
    VENUES_DETAIL: (id: string) => `/venues/${id}`,
    VENUES_DETAILS_PROMOTION_CREATE: (venueId: string) =>
      `/venues/${venueId}/promotions/create`,
    VENUES_DETAILS_PROMOTION_EDIT: (venueId: string, promoId: string) =>
      `/venues/${venueId}/promotions/${promoId}/edit`,
    VENUES_DETAILS_DISCOUNT_CREATE: (venueId: string) =>
      `/venues/${venueId}/discounts/create`,
    VENUES_DETAILS_DISCOUNT_EDIT: (venueId: string, discountId: string) =>
      `/venues/${venueId}/discounts/${discountId}/edit`,
    VENUES_CREATE: (companyId: string) =>
      `/companies/${companyId}/venues/create`,
    VENUES_EDIT: (id: string) => `/venues/${id}/edit`,
    VENUES_DETAILS_ADVANCED: (venueId: string) => `/venues/${venueId}/advanced`,
    DASHBOARD: '/dashboard',
    PROFILE: '/profile',
  },
} as const;

export const API_ENDPOINTS = {
  AUTH: {
    SIGNIN: '/api/v1/auth/signin',
    SIGNUP: '/api/v1/auth/signup',
    CONFIRM_EMAIL: '/api/v1/auth/confirm-email',
    RESEND_CONFIRMATION_EMAIL: '/api/v1/auth/resend-confirmation-email',
    LOGOUT: '/api/v1/auth/logout',
    REQUEST_PASSWORD_RESET: '/api/v1/auth/request-password-reset',
    CONFIRM_PASSWORD_RESET: '/api/v1/auth/confirm-password-reset',
    RENEW_TOKENS: '/api/v1/auth/renew-tokens',
    GOOGLE: '/api/v1/auth/google',
    GET_ACCOUNT: '/api/v1/auth/account',
    DELETE_ACCOUNT: '/api/v1/auth/account',
  },
  USER_PROFILE: {
    PROFILE: '/api/v1/user-profile',
    PRIVACY: '/api/v1/user-profile/privacy',
    AVATAR_UPLOAD_URL: '/api/v1/user-profile/avatar/upload-url',
    COVER_UPLOAD_URL: '/api/v1/user-profile/cover/upload-url',
  },
  HOBBIES: {
    LIST: '/api/v1/hobby',
  },
  TAGS: {
    LIST: '/api/v1/tags',
  },
  FEED: {
    STATUS: '/api/v1/feed/status',
    LIST: '/api/v1/feed',
    RATE: (userId: string) => `/api/v1/feed/${userId}/rating`,
    INVITE: (userId: string) => `/api/v1/feed/${userId}/invite`,
    ACCEPT_INVITE: (userId: string) => `/api/v1/feed/${userId}/accept-invite`,
    INVITE_EXPIRATION: (userId: string) =>
      `/api/v1/feed/${userId}/invite-expiration`,
    BLOCK: (userId: string) => `/api/v1/feed/${userId}/block`,
    UNBLOCK: (userId: string) => `/api/v1/feed/${userId}/unblock`,
    SKIP: (userId: string) => `/api/v1/feed/${userId}/skip`,
    IGNORE: (userId: string) => `/api/v1/feed/${userId}/ignore`,
    FILTER: '/api/v1/feed/filter',
    BLOCKED_USERS: '/api/v1/feed/blocked-users',
    FRIENDS: '/api/v1/feed/friends',
    INVITE_FRIEND: (userId: string) => `/api/v1/feed/friends/${userId}/invite`,
    REMOVE_FRIEND: (userId: string) => `/api/v1/feed/${userId}/remove-friend`,
    SETTINGS: '/api/v1/feed/settings',
  },
  NOTIFICATIONS: {
    LIST: '/api/v1/notifications',
    UNSEEN_COUNT: '/api/v1/notifications/unseen/count',
    SETTINGS: '/api/v1/notifications/settings',
    FCM_TOKEN: '/api/v1/notifications/device/token',
    DELETE: (id: string) => `/api/v1/notifications/${id}`,
  },
  CHAT: {
    GET: '/api/v1/chat',
    SEND_MESSAGE: '/api/v1/chat/message',
    DELETE: '/api/v1/chat',
  },
  LOCATION: {
    UPDATE: '/api/v1/location',
  },
  B2B: {
    COMPANIES: {
      BASE: '/api/v1/b2b/companies',
      BY_ID: (id: string) => `/api/v1/b2b/company/${id}`,
      AVATAR_UPLOAD_URL: (id: string) =>
        `/api/v1/b2b/company/${id}/avatar/upload-url`,
      VENUE: (id: string) => `/api/v1/b2b/company/${id}/venue`,
    },
    VENUES: {
      BASE: '/api/v1/b2b/venues',
      DETAILS: (id: string) => `/api/v1/b2b/venue/${id}`,
      COVER_UPLOAD_URL: (id: string) =>
        `/api/v1/b2b/venue/${id}/cover/upload-url`,
      PROMOTIONS: (venueId: string) =>
        `/api/v1/b2b/venue/${venueId}/promotions`,
      PROMOTION: (venueId: string, promoId: string) =>
        `/api/v1/b2b/venue/${venueId}/promotions/${promoId}`,
      DISCOUNTS: (venueId: string) => `/api/v1/b2b/venue/${venueId}/discounts`,
      DISCOUNT: (venueId: string, discountId: string) =>
        `/api/v1/b2b/venue/${venueId}/discounts/${discountId}`,
    },
    LOYALTY_LEVELS: {
      BY_COMPANY_ID: (companyId: string) =>
        `/api/v1/b2b/company/${companyId}/loyalty-levels`,
    },
    TAGS: {
      LIST: '/api/v1/b2b/tags',
    },
    DASHBOARD: {
      VISITS: '/api/v1/b2b/dashboard/visits',
    },
  },
  VENUES: {
    LIST: '/api/v1/venues',
    MAP: '/api/v1/venues/map',
    QR_CHECK_IN: (qrCode: string) => `/api/v1/venue/${qrCode}/qr-check-in`,
    QR_CHECK_IN_SETTINGS: (qrCode: string) =>
      `/api/v1/venue/${qrCode}/qr-check-in/settings`,
    DETAILS: (id: string) => `/api/v1/venue/${id}`,
    DISCOUNTS: (venueId: string) => `/api/v1/venue/${venueId}/discounts`,
  },
  DEALS: {
    DISCOUNTS: '/api/v1/discounts',
    PROMOTIONS: '/api/v1/promotions',
  },
  NPC: {
    GET: '/api/v1/npc',
    MET: '/api/v1/npc/met',
    INVITE: (id: string) => `/api/v1/npc/${id}/invite`,
  },
} as const;
