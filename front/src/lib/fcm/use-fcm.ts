'use client';

import { useEffect, useCallback, useRef } from 'react';
import { initializeApp, getApps } from 'firebase/app';
import {
  getMessaging,
  getToken,
  onMessage,
  deleteToken,
} from 'firebase/messaging';

import { useFcmStore } from './store';
import { FcmState, StoredToken } from './types';
import {
  createForegroundMessageHandler,
  createSwMessageListener,
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
const TOKEN_TTL = 30 * 24 * 60 * 60 * 1000;

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

function isRetriableError(error: any): boolean {
  const message = error?.message || '';
  const code = error?.code || '';

  // Permanent errors (do not retry)
  if (
    code === 'messaging/permission-blocked' ||
    code === 'messaging/unsupported-browser' ||
    code === 'auth/invalid-api-key' ||
    message.includes('permission') ||
    message.includes('unsupported')
  ) {
    return false;
  }

  // Assume other errors (network, timeout, unknown) are retriable
  return true;
}

/* ------------------------------------------------------------------ */
/* Hook */
/* ------------------------------------------------------------------ */

export function useFcm(hasOnboardingCompleted: boolean = false) {
  const {
    setState: setFcmState,
    setApp,
    setMessaging,
    setSwRegistration,
    setUnsubForeground,
    setSwMessageListener,
    reset: resetFcmStore,
  } = useFcmStore();

  /* -------------------------------------------------------------- */
  /* Message handling (created once, reads from store dynamically) */
  /* -------------------------------------------------------------- */

  const onForegroundMessage = useCallback(createForegroundMessageHandler(), []);

  /* -------------------------------------------------------------- */
  /* State machine runner */
  /* -------------------------------------------------------------- */

  // Module-level transition mutex
  let transitionMutex: Promise<void> | null = null;
  const retryCount = useRef(0);
  const retryTimer = useRef<NodeJS.Timeout | null>(null);
  const isRetriableRef = useRef(true);

  const transition = useCallback(
    async (next: FcmState) => {
      // Module-level mutex: queue transitions to prevent race conditions
      const previousMutex = transitionMutex;
      let resolveMutex: () => void;
      transitionMutex = new Promise((resolve) => {
        resolveMutex = resolve;
      });

      // Wait for any previous transition to complete
      if (previousMutex) {
        await previousMutex;
      }

      try {
        const store = useFcmStore.getState();

        // Prevent unnecessary state updates if we are already in the target state
        if (store.state === next) {
          if (resolveMutex!) resolveMutex();
          return;
        }

        setFcmState(next);
        console.log('[FCM]', next);

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
            await navigator.serviceWorker.ready;
            const reg = await navigator.serviceWorker.getRegistration();
            if (reg) {
              setSwRegistration(reg);
              transition(FcmState.TOKEN);
            } else {
              console.warn('[FCM] SW not ready yet');
              // Retrying via visibility change or next render
            }
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

            if (!currentSwReg) {
              console.error('[FCM] SW registration missing');
              transition(FcmState.ERROR);
              return;
            }

            // Always fetch current token to detect refreshes
            const currentToken = await getToken(msg, {
              vapidKey: VAPID_KEY,
              serviceWorkerRegistration: currentSwReg,
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
              const callbacks = useFcmStore.getState().apiCallbacks;
              if (callbacks) {
                await callbacks.onTokenRegister(getDeviceId(), currentToken);
              } else {
                console.warn(
                  '[FCM] API callbacks not set, skipping token registration',
                );
              }

              saveToken(currentToken);
              console.log('[FCM] Token registered/refreshed');
            }

            const unsub = onMessage(msg, onForegroundMessage);
            setUnsubForeground(unsub);

            // Create and track SW message listener
            const swListener = createSwMessageListener();
            setSwMessageListener(swListener);
            navigator.serviceWorker.addEventListener('message', swListener);

            transition(FcmState.READY);
            break;
          }

          case FcmState.READY:
            retryCount.current = 0;
            break;

          case FcmState.ERROR:
            console.error('[FCM] ERROR state');
            if (retryCount.current < 5 && isRetriableRef.current) {
              const delay = Math.pow(2, retryCount.current) * 1000;
              console.log(`[FCM] Retrying initialization in ${delay}ms...`);
              retryTimer.current = setTimeout(() => {
                retryCount.current += 1;
                transition(FcmState.IDLE);
              }, delay);
            } else {
              console.error(
                `[FCM] Stopped retrying. Max retries: ${retryCount.current >= 5}, Retriable: ${isRetriableRef.current}`,
              );
            }
            break;
        }
      } catch (e) {
        console.error('[FCM] transition failed', e);
        isRetriableRef.current = isRetriableError(e);
        transition(FcmState.ERROR);
      } finally {
        if (resolveMutex!) resolveMutex();
      }
    },
    [
      setFcmState,
      setApp,
      setMessaging,
      setSwRegistration,
      setUnsubForeground,
      setSwMessageListener,
      onForegroundMessage,
    ],
  );

  /* -------------------------------------------------------------- */
  /* Auth -> state transitions */
  /* -------------------------------------------------------------- */

  useEffect(() => {
    console.log('[useFcm] Effect. Allowed:', hasOnboardingCompleted);

    if (hasOnboardingCompleted) {
      transition(FcmState.PERMISSION);
    } else {
      (async () => {
        // Remove foreground listener
        const unsub = useFcmStore.getState().unsubForeground;
        unsub?.();
        setUnsubForeground(null);

        // Remove SW listener using tracked reference
        const swListener = useFcmStore.getState().swMessageListener;
        if (swListener) {
          navigator.serviceWorker.removeEventListener('message', swListener);
          setSwMessageListener(null);
        }

        // Delete FCM token
        const currentMessaging = useFcmStore.getState().messaging;
        if (currentMessaging) {
          try {
            await deleteToken(currentMessaging);
            const callbacks = useFcmStore.getState().apiCallbacks;
            if (callbacks) {
              await callbacks.onTokenUnregister(getDeviceId());
            }
          } catch (err) {
            console.error('[FCM] Token unregister failed:', err);
          }
        }

        clearToken();
        resetFcmStore();
      })();
    }

    return () => {
      const swListener = useFcmStore.getState().swMessageListener;
      if (swListener) {
        navigator.serviceWorker.removeEventListener('message', swListener);
      }
      const unsub = useFcmStore.getState().unsubForeground;
      unsub?.();
    };
  }, [
    hasOnboardingCompleted,
    transition,
    setUnsubForeground,
    setSwMessageListener,
    resetFcmStore,
  ]);

  // Cleanup retry timer on unmount
  useEffect(() => {
    return () => {
      if (retryTimer.current) {
        clearTimeout(retryTimer.current);
      }
    };
  }, []);

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
        const callbacks = useFcmStore.getState().apiCallbacks;
        if (callbacks) {
          await callbacks.onTokenRegister(getDeviceId(), currentToken);
        }

        saveToken(currentToken);
        console.log('[FCM] Token refreshed and updated');
      }
    } catch (err) {
      console.error('[FCM] Token refresh check failed:', err);
    }
  }, []);

  useEffect(() => {
    if (!hasOnboardingCompleted) return;

    let debounceTimer: ReturnType<typeof setTimeout> | null = null;

    const handleVisibilityChange = () => {
      if (document.visibilityState !== 'visible') return;

      // Debounce to prevent spam on rapid tab switching
      if (debounceTimer) clearTimeout(debounceTimer);
      debounceTimer = setTimeout(() => {
        const currentState = useFcmStore.getState().state;

        // If we are already READY, just check for token refresh
        if (currentState === FcmState.READY) {
          checkTokenRefresh();
        } else if (
          currentState === FcmState.IDLE ||
          currentState === FcmState.ERROR
        ) {
          // Only re-trigger if permission is already granted (don't re-prompt)
          if (Notification.permission === 'granted') {
            transition(FcmState.PERMISSION);
          }
        }
      }, 500);
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      if (debounceTimer) clearTimeout(debounceTimer);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [hasOnboardingCompleted, checkTokenRefresh, transition]);
}
