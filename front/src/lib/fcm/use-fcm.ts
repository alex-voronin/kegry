'use client';

import { useEffect, useRef, useCallback } from 'react';
import { initializeApp, getApps } from 'firebase/app';
import {
  getMessaging,
  getToken,
  onMessage,
  deleteToken,
} from 'firebase/messaging';
import axios from 'axios';

import { useRouter } from '@/i18n/routing';
import { API_ENDPOINTS } from '@/lib/routes';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { useFeedStore } from '@/store/use-feed-store';

import { useFcmStore } from './store';
import { FcmState, StoredToken } from './types';
import {
  createForegroundMessageHandler,
  createBackgroundMessageFactory,
  getBackgroundMessageHandler,
  clearBackgroundMessageHandler,
} from './handlers';

/* ------------------------------------------------------------------ */
/* Config */
/* ------------------------------------------------------------------ */

const firebaseConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY!,
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN!,
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID!,
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID!,
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID!,
};

const VAPID_KEY = process.env.NEXT_PUBLIC_FIREBASE_VAPID_KEY!;
const TOKEN_STORAGE_KEY = 'fcm_token_info';
const DEVICE_ID_KEY = 'fcm_device_id';
const TOKEN_TTL = 7 * 24 * 60 * 60 * 1000;

/* ------------------------------------------------------------------ */
/* Utils */
/* ------------------------------------------------------------------ */

function isBrowser(): boolean {
  return typeof window !== 'undefined';
}

function getDeviceId(): string {
  if (!isBrowser()) return '';

  let id = localStorage.getItem(DEVICE_ID_KEY);
  if (!id) {
    id = crypto.randomUUID?.() || `${Date.now()}-${Math.random()}`;
    localStorage.setItem(DEVICE_ID_KEY, id);
  }
  return id;
}

function saveToken(token: string) {
  if (!isBrowser()) return;
  localStorage.setItem(
    TOKEN_STORAGE_KEY,
    JSON.stringify({ token, ts: Date.now() }),
  );
}

function loadToken(): StoredToken | null {
  if (!isBrowser()) return null;
  try {
    const item = localStorage.getItem(TOKEN_STORAGE_KEY);
    return item ? JSON.parse(item) : null;
  } catch {
    return null;
  }
}

function clearToken() {
  if (!isBrowser()) return;
  localStorage.removeItem(TOKEN_STORAGE_KEY);
}

function isTokenValid(): boolean {
  const stored = loadToken();
  if (!stored) return false;
  return Date.now() - stored.ts < TOKEN_TTL;
}

/* ------------------------------------------------------------------ */
/* Service Worker */
/* ------------------------------------------------------------------ */

async function registerServiceWorker(): Promise<ServiceWorkerRegistration> {
  const reg = await navigator.serviceWorker.register(
    '/firebase-messaging-sw.js',
  );
  await navigator.serviceWorker.ready;
  return reg;
}

/* ------------------------------------------------------------------ */
/* Hook */
/* ------------------------------------------------------------------ */

export function useFcm() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const requiredAction = useAuthStore((s) => s.requiredAction);

  const canInitializeFcm =
    !!accessToken && requiredAction === RequiredAction.None;

  const routerRef = useRef(router);
  useEffect(() => {
    routerRef.current = router;
  }, [router]);

  const messaging = useFcmStore((s) => s.messaging);
  const swRegistration = useFcmStore((s) => s.swRegistration);
  const unsubForeground = useFcmStore((s) => s.unsubForeground);
  const customForegroundHandlers = useFcmStore(
    (s) => s.customForegroundHandlers,
  );
  const backgroundMessageHandler = useFcmStore(
    (s) => s.backgroundMessageHandler,
  );

  const {
    setState: setFcmState,
    setApp,
    setMessaging,
    setSwRegistration,
    setUnsubForeground,
    reset: resetFcmStore,
  } = useFcmStore();

  /* -------------------------------------------------------------- */
  /* Message handling */
  /* -------------------------------------------------------------- */

  const isInMeeting = useFeedStore((s) => s.isInMeeting);

  const onForegroundMessage = useCallback(
    createForegroundMessageHandler({
      router,
      isInMeeting,
      swRegistration,
      customHandlers: customForegroundHandlers,
    }),
    [router, isInMeeting, swRegistration, customForegroundHandlers],
  );

  const onBackgroundMessageFactory = useCallback(
    createBackgroundMessageFactory({
      routerRef,
      customHandler: backgroundMessageHandler,
    }),
    [backgroundMessageHandler],
  );

  /* -------------------------------------------------------------- */
  /* State machine runner */
  /* -------------------------------------------------------------- */

  const transition = useCallback(
    async (next: FcmState) => {
      const currentState = useFcmStore.getState().state;
      if (currentState === next) return;
      setFcmState(next);
      console.log('[FCM]', next);

      try {
        switch (next) {
          case FcmState.PERMISSION: {
            if (!isBrowser() || !('Notification' in window)) {
              transition(FcmState.IDLE);
              return;
            }

            if (Notification.permission === 'denied') {
              transition(FcmState.IDLE);
              return;
            }

            if (Notification.permission !== 'granted') {
              const p = await Notification.requestPermission();
              if (p !== 'granted') {
                transition(FcmState.IDLE);
                return;
              }
            }

            transition(FcmState.SW);
            break;
          }

          case FcmState.SW: {
            const reg = await registerServiceWorker();
            setSwRegistration(reg);
            transition(FcmState.TOKEN);
            break;
          }

          case FcmState.TOKEN: {
            const app =
              getApps().length === 0
                ? initializeApp(firebaseConfig)
                : getApps()[0];

            setApp(app);

            const msg = getMessaging(app);
            setMessaging(msg);

            const currentSwReg = useFcmStore.getState().swRegistration;

            // Always fetch current token to detect refreshes
            const currentToken = await getToken(msg, {
              vapidKey: VAPID_KEY,
              serviceWorkerRegistration: currentSwReg!,
            });

            if (!currentToken) {
              console.warn('[FCM] Token empty, will retry on next trigger');
              transition(FcmState.IDLE);
              return;
            }

            const stored = loadToken();
            const hasTokenChanged = !stored || stored.token !== currentToken;

            // Register/update token if changed or expired
            if (hasTokenChanged || !isTokenValid()) {
              await axios.post(API_ENDPOINTS.NOTIFICATIONS.FCM_TOKEN, {
                deviceId: getDeviceId(),
                fcmToken: currentToken,
              });

              saveToken(currentToken);
              console.log('[FCM] Token registered/refreshed');
            }

            const unsub = onMessage(msg, onForegroundMessage);
            setUnsubForeground(unsub);

            const handler = onBackgroundMessageFactory();
            navigator.serviceWorker.addEventListener('message', handler);

            transition(FcmState.READY);
            break;
          }

          case FcmState.READY:
            break;

          case FcmState.ERROR:
            console.error('[FCM] ERROR state');
            break;
        }
      } catch (e) {
        console.error('[FCM] transition failed', e);
        transition(FcmState.ERROR);
      }
    },
    [
      setFcmState,
      setApp,
      setMessaging,
      setSwRegistration,
      setUnsubForeground,
      onForegroundMessage,
      onBackgroundMessageFactory,
    ],
  );

  /* -------------------------------------------------------------- */
  /* Auth -> state transitions */
  /* -------------------------------------------------------------- */

  useEffect(() => {
    if (canInitializeFcm) {
      transition(FcmState.PERMISSION);
    } else {
      (async () => {
        // Remove foreground listener
        unsubForeground?.();
        setUnsubForeground(null);

        // Remove SW listener using stable reference
        const bgHandler = getBackgroundMessageHandler();
        if (bgHandler) {
          navigator.serviceWorker.removeEventListener('message', bgHandler);
          clearBackgroundMessageHandler();
        }

        // Delete FCM token
        if (messaging) {
          try {
            await deleteToken(messaging);
            await axios.delete(API_ENDPOINTS.NOTIFICATIONS.FCM_TOKEN, {
              params: { deviceId: getDeviceId() },
            });
          } catch (err) {
            console.error('[FCM] Token unregister failed:', err);
          }
        }

        clearToken();
        resetFcmStore();
      })();
    }

    return () => {
      const handler = getBackgroundMessageHandler();
      if (handler) {
        navigator.serviceWorker.removeEventListener('message', handler);
      }
      unsubForeground?.();
    };
  }, [
    canInitializeFcm,
    transition,
    unsubForeground,
    messaging,
    setUnsubForeground,
    resetFcmStore,
  ]);

  /* -------------------------------------------------------------- */
  /* Token refresh on visibility change */
  /* -------------------------------------------------------------- */

  const checkTokenRefresh = useCallback(async () => {
    const currentMessaging = useFcmStore.getState().messaging;
    const currentSwReg = useFcmStore.getState().swRegistration;
    const currentState = useFcmStore.getState().state;

    if (!currentMessaging || !currentSwReg) return;
    if (currentState !== FcmState.READY) return;

    try {
      const currentToken = await getToken(currentMessaging, {
        vapidKey: VAPID_KEY,
        serviceWorkerRegistration: currentSwReg,
      });

      if (!currentToken) return;

      const stored = loadToken();
      if (!stored || stored.token !== currentToken || !isTokenValid()) {
        await axios.post(API_ENDPOINTS.NOTIFICATIONS.FCM_TOKEN, {
          deviceId: getDeviceId(),
          fcmToken: currentToken,
        });

        saveToken(currentToken);
        console.log('[FCM] Token refreshed and updated');
      }
    } catch (err) {
      console.error('[FCM] Token refresh check failed:', err);
    }
  }, []);

  useEffect(() => {
    if (!canInitializeFcm) return;

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        const currentState = useFcmStore.getState().state;

        // If we are already READY, just check for token refresh
        if (currentState === FcmState.READY) {
          checkTokenRefresh();
        } else if (
          currentState === FcmState.IDLE ||
          currentState === FcmState.ERROR
        ) {
          // If we were stuck in IDLE or ERROR (e.g. permission was denied)
          // re-trigger the flow.
          transition(FcmState.PERMISSION);
        }
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [canInitializeFcm, checkTokenRefresh, transition]);
}
