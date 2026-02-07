'use client';

import { ReactNode, useEffect, useCallback } from 'react';
import { NextIntlClientProvider } from 'next-intl';
import axios from 'axios';
import { useRouter } from '@/i18n/routing';
import { useBackgroundLocation } from '@/lib/use-background-location';
import { useFcm } from '@/lib/fcm/use-fcm';
import { useFcmStore } from '@/lib/fcm/store';
import { useFeedStore } from '@/store/use-feed-store';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { FcmPayload, NotificationType } from '@/lib/fcm/types';
import { TawkMessenger } from './tawk-messenger';

function FcmGlobalHandler() {
  const router = useRouter();
  const {
    registerForegroundHandler,
    unregisterForegroundHandler,
    setBackgroundMessageHandler,
    setApiCallbacks,
    setStateCallbacks,
  } = useFcmStore();

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
              registration.showNotification(
                notification.title || 'Hoppyfy.me',
                {
                  body: notification.body || '',
                  icon:
                    notification.icon ||
                    '/assets/notifications/notification-logo.webp',
                  data: payload,
                },
              );
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
    // Register state callbacks
    setStateCallbacks({
      getIsFcmAllowed: () => {
        const { accessToken, requiredAction } = useAuthStore.getState();
        return !!accessToken && requiredAction === RequiredAction.None;
      },
    });

    // Register API callbacks
    setApiCallbacks({
      onTokenRegister: async (deviceId, fcmToken) => {
        await axios.post(API_ENDPOINTS.NOTIFICATIONS.FCM_TOKEN, {
          deviceId,
          fcmToken,
        });
      },
      onTokenUnregister: async (deviceId) => {
        await axios.delete(API_ENDPOINTS.NOTIFICATIONS.FCM_TOKEN, {
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
      setStateCallbacks(null);
    };
  }, [
    registerForegroundHandler,
    unregisterForegroundHandler,
    setBackgroundMessageHandler,
    setApiCallbacks,
    setStateCallbacks,
    handleForegroundMessage,
    handleBackgroundMessage,
  ]);

  return null;
}

function BackgroundServices() {
  useBackgroundLocation();
  useFcm();
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
  return (
    <NextIntlClientProvider messages={messages} locale={locale} timeZone="UTC">
      <TawkMessenger />
      <BackgroundServices />
      <FcmGlobalHandler />
      {children}
    </NextIntlClientProvider>
  );
}
