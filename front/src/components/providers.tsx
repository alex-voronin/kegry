'use client';

import { ReactNode, useEffect, useCallback } from 'react';
import { NextIntlClientProvider, useLocale } from 'next-intl';
import apiClient, {
  setAcceptLanguage as setApiClientLanguage,
  setTimezone as setApiClientTimezone,
} from '@/lib/api-client';
import { setAcceptLanguage as setAuthClientLanguage } from '@/lib/auth-client';
import { useRouter, usePathname } from '@/i18n/routing';
import { useBackgroundLocation } from '@/lib/use-background-location';
import { useLocationWarmer } from '@/lib/use-location-warmer';
import { useFcm } from '@/lib/fcm/use-fcm';
import { useFcmStore } from '@/lib/fcm/store';
import { useFeedStore } from '@/app/b2c/[locale]/feed/use-feed-store';
import {
  useAuthStore,
  hasCompletedOnboarding,
} from '@/lib/auth/use-auth-store';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { FcmPayload, NotificationType } from '@/lib/fcm/types';
import { TawkMessenger } from './tawk-messenger';
import { BRAND_NAME } from '@/lib/constants';
import { NavigationTracker } from './common/NavigationTracker';
import { DayjsLocaleProvider } from './DayjsLocaleProvider';

function FcmGlobalHandler() {
  const router = useRouter();
  const pathname = usePathname();
  const {
    registerForegroundHandler,
    unregisterForegroundHandler,
    setBackgroundMessageHandler,
    setApiCallbacks,
  } = useFcmStore();

  const handleForegroundMessage = useCallback(
    (payload: FcmPayload) => {
      const data = payload?.data || {};
      const type = data.type;
      const messageId = data.messageId;
      const isInMeeting = useFeedStore.getState().isInMeeting;

      // Navigation actions based on type
      if (type === NotificationType.MEETING_ARRANGED) {
        router.push(PAGE_ROUTES.CHAT);
      }
      if (type === NotificationType.OPEN_RATE_USER_SCREEN) {
        const userId = data.rateeId;
        if (userId) {
          router.push(PAGE_ROUTES.RATE_USER(userId));
        }
      }

      let showNotification = false;
      // Show system notification for invitations (when not in meeting)
      if (type === NotificationType.USER_INVITED && !isInMeeting) {
        showNotification = true;
      }

      if (type === NotificationType.REFERRER_REWARD_RECEIVED) {
        showNotification = true;
      }

      if (type === NotificationType.PARTICIPATION_APPROVED) {
        showNotification = true;
      }

      if (type === NotificationType.USER_REQUESTED_APPROVE) {
        showNotification = true;
      }

      if (
        type === NotificationType.CHAT_MESSAGE_RECEIVED &&
        pathname !== PAGE_ROUTES.CHAT
      ) {
        showNotification = true;
      }

      if (showNotification) {
        if (
          'serviceWorker' in navigator &&
          navigator.serviceWorker.controller
        ) {
          navigator.serviceWorker.ready
            .then(async (registration) => {
              if (!data) return;

              try {
                const tag = `${type}-${messageId}`; // tag for deduplication
                await registration.showNotification(data.title || BRAND_NAME, {
                  body: data.body || '',
                  icon:
                    data.icon || '/assets/notifications/notification-logo.webp',
                  badge: '/assets/notifications/notification-badge.png', // webp is not supported for badges
                  tag: tag,
                  data: payload,
                });
              } catch (error) {
                console.error('Failed to show notification:', error);
              }
            })
            .catch((err) => {
              console.error(
                'Service Worker ready failed or notification error:',
                err,
              );
            });
        }
      }
    },
    [router, pathname],
  );

  const handleBackgroundMessage = useCallback(
    (payload: FcmPayload) => {
      const type = payload?.data?.type;

      const isInMeeting = useFeedStore.getState().isInMeeting;
      if (type === NotificationType.USER_INVITED && !isInMeeting) {
        router.replace(PAGE_ROUTES.NOTIFICATIONS);
      }
      if (
        type === NotificationType.MEETING_ARRANGED ||
        type === NotificationType.CHAT_MESSAGE_RECEIVED ||
        type === NotificationType.SHOW_END_MEETING_SUGGESTION
      ) {
        router.replace(PAGE_ROUTES.CHAT);
      }

      if (type === NotificationType.OPEN_RATE_USER_SCREEN) {
        router.replace(PAGE_ROUTES.FEED);
      }

      if (type === NotificationType.REFERRER_REWARD_RECEIVED) {
        router.replace(`${PAGE_ROUTES.FRIENDS_AND_FOES}?tab=imaginary`);
      }

      if (type === NotificationType.USER_REQUESTED_APPROVE) {
        const eventId = payload?.data?.eventId;
        if (eventId) {
          router.replace(PAGE_ROUTES.MEETUP_DETAIL(eventId));
        }
      }

      if (type === NotificationType.PARTICIPATION_APPROVED) {
        const eventId = payload?.data?.eventId;
        if (eventId) {
          router.replace(PAGE_ROUTES.MEETUP_DETAIL(eventId));
        }
      }
    },
    [router],
  );

  useEffect(() => {
    // Register API callbacks
    setApiCallbacks({
      onTokenRegister: async (deviceId, fcmToken) => {
        await apiClient.post(API_ENDPOINTS.NOTIFICATIONS.FCM_TOKEN, {
          deviceId,
          fcmToken,
        });
      },
      onTokenUnregister: async (deviceId) => {
        await apiClient.delete(API_ENDPOINTS.NOTIFICATIONS.FCM_TOKEN, {
          params: { deviceId },
        });
      },
    });

    // Register message handlers
    registerForegroundHandler(handleForegroundMessage);
    setBackgroundMessageHandler(handleBackgroundMessage);

    return () => {
      unregisterForegroundHandler(handleForegroundMessage);
      setBackgroundMessageHandler(null);
      setApiCallbacks(null);
    };
  }, [
    registerForegroundHandler,
    unregisterForegroundHandler,
    setBackgroundMessageHandler,
    setApiCallbacks,
    handleForegroundMessage,
    handleBackgroundMessage,
  ]);

  return null;
}

import { registerServiceWorker } from '@/lib/service-worker';

function BackgroundServices() {
  const isOnboardingCompleted = useAuthStore(hasCompletedOnboarding);
  useBackgroundLocation();
  useLocationWarmer();
  useFcm(isOnboardingCompleted);

  useEffect(() => {
    registerServiceWorker().catch((err) =>
      console.error('SW registration failed', err),
    );
  }, []);

  return null;
}

function LocaleHeaderSync() {
  const locale = useLocale();

  useEffect(() => {
    setApiClientLanguage(locale);
    setAuthClientLanguage(locale);
  }, [locale]);

  return null;
}

function TimezoneHeaderSync() {
  useEffect(() => {
    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    setApiClientTimezone(timezone);
  }, []);

  return null;
}

export function Providers({
  children,
  runPushesAndLocationHandling,
  locale,
  messages,
}: {
  children: ReactNode;
  runPushesAndLocationHandling: boolean;
  locale: string;
  messages: Record<string, any>;
}) {
  const canMountGlobalHandlers = useAuthStore(hasCompletedOnboarding);
  console.log(
    '[Providers] Render. canMountGlobalHandlers:',
    canMountGlobalHandlers,
  );

  return (
    <NextIntlClientProvider messages={messages} locale={locale} timeZone="UTC">
      <NavigationTracker />
      <DayjsLocaleProvider />
      <LocaleHeaderSync />
      <TimezoneHeaderSync />
      <TawkMessenger />
      {runPushesAndLocationHandling && (
        <>
          {canMountGlobalHandlers && <FcmGlobalHandler />}
          <BackgroundServices />
        </>
      )}
      {children}
    </NextIntlClientProvider>
  );
}
