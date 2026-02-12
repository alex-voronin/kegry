'use client';

import { ReactNode, useEffect, useCallback } from 'react';
import { NextIntlClientProvider } from 'next-intl';
import apiClient from '@/lib/api-client';
import { useRouter } from '@/i18n/routing';
import { useBackgroundLocation } from '@/lib/use-background-location';
import { useFcm } from '@/lib/fcm/use-fcm';
import { useFcmStore } from '@/lib/fcm/store';
import { useFeedStore } from '@/store/use-feed-store';
import {
  useAuthStore,
  RequiredAction,
  hasCompleteOnboarding,
} from '@/lib/auth/use-auth-store';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { FcmPayload, NotificationType } from '@/lib/fcm/types';
import { TawkMessenger } from './tawk-messenger';
import { BRAND_NAME } from '@/lib/constants';

function FcmGlobalHandler() {
  const router = useRouter();
  const {
    registerForegroundHandler,
    unregisterForegroundHandler,
    setBackgroundMessageHandler,
    setApiCallbacks,
    setIsFcmAllowed,
  } = useFcmStore();

  useEffect(() => {
    // Subscribe to auth changes to sync FCM allowed state without re-rendering this component
    return useAuthStore.subscribe((state) => {
      setIsFcmAllowed(hasCompleteOnboarding(state));
    });
  }, [setIsFcmAllowed]);

  const handleForegroundMessage = useCallback(
    (payload: FcmPayload) => {
      const data = payload?.data || {};
      const type = data.type;
      const notification = payload?.notification;
      const isInMeeting = useFeedStore.getState().isInMeeting;

      // Navigation actions based on type
      if (type === NotificationType.MEETING_ARRANGED) {
        router.push(PAGE_ROUTES.CHAT);
      }
      if (type === NotificationType.OPEN_RATE_USER_SCREEN) {
        const userId = data.RateeId;
        if (userId) {
          router.push(PAGE_ROUTES.RATE_USER(userId));
        }
      }

      // Show system notification for invitations (when not in meeting)
      if (type === NotificationType.USER_INVITED && !isInMeeting) {
        if (
          'serviceWorker' in navigator &&
          navigator.serviceWorker.controller
        ) {
          navigator.serviceWorker.ready.then((registration) => {
            if (notification) {
              registration.showNotification(notification.title || BRAND_NAME, {
                body: notification.body || '',
                icon:
                  notification.icon ||
                  '/assets/notifications/notification-logo.webp',
                data: payload,
              });
            }
          });
        }
      }
    },
    [router],
  );

  const handleBackgroundMessage = useCallback(
    (payload: FcmPayload) => {
      const type = payload?.data?.type;

      if (type === NotificationType.USER_INVITED) {
        router.push(PAGE_ROUTES.NOTIFICATIONS);
      }
      if (
        type === NotificationType.MEETING_ARRANGED ||
        type === NotificationType.CHAT_MESSAGE_RECEIVED
      ) {
        router.push(PAGE_ROUTES.CHAT);
      }
    },
    [router],
  );

  useEffect(() => {
    // Sync allowed state
    setIsFcmAllowed(hasCompleteOnboarding(useAuthStore.getState()));

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
      setIsFcmAllowed(false);
    };
  }, [
    registerForegroundHandler,
    unregisterForegroundHandler,
    setBackgroundMessageHandler,
    setApiCallbacks,
    setIsFcmAllowed,
    handleForegroundMessage,
    handleBackgroundMessage,
  ]);

  return null;
}

import { registerServiceWorker } from '@/lib/service-worker';

function BackgroundServices() {
  useBackgroundLocation();
  useFcm();

  useEffect(() => {
    registerServiceWorker().catch((err) =>
      console.error('SW registration failed', err),
    );
  }, []);

  return null;
}

export function Providers({
  children,
  locale,
  messages,
}: {
  children: ReactNode;
  locale: string;
  messages: Record<string, any>;
}) {
  const canMountGlobalHandlers = useAuthStore(hasCompleteOnboarding);

  return (
    <NextIntlClientProvider messages={messages} locale={locale} timeZone="UTC">
      <TawkMessenger />
      {canMountGlobalHandlers && <FcmGlobalHandler />}
      <BackgroundServices />
      {children}
    </NextIntlClientProvider>
  );
}
