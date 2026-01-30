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
import { FcmPayload } from '@/lib/fcm/types';

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
      const action = data.action;
      const userAction = data.userAction;
      const notification = payload?.notification;
      const isInMeeting = useFeedStore.getState().isInMeeting;

      // Navigation actions
      if (action === 'openChatScreen') {
        router.push(PAGE_ROUTES.CHAT);
      }
      if (action === 'openRateUserScreen') {
        router.push(PAGE_ROUTES.RATE_USER);
      }

      // Show system notification for notifications screen (when not in meeting)
      if (userAction === 'openNotificationsScreen' && !isInMeeting) {
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
                  icon: notification.icon || '/assets/logo.png',
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
      const userAction = payload?.data?.userAction;

      if (userAction === 'openNotificationsScreen') {
        router.push(PAGE_ROUTES.NOTIFICATIONS);
      }
      if (userAction === 'openChatScreen') {
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
      <BackgroundServices />
      <FcmGlobalHandler />
      {children}
    </NextIntlClientProvider>
  );
}
