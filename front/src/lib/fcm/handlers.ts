'use client';

import { FcmPayload, FcmMessageHandler } from './types';

/* ------------------------------------------------------------------ */
/* Types */
/* ------------------------------------------------------------------ */

export interface ForegroundMessageContext {
  router: { push: (path: string) => void };
  isInMeeting: boolean;
  swRegistration: ServiceWorkerRegistration | null;
  customHandlers: FcmMessageHandler[];
}

export interface BackgroundMessageContext {
  routerRef: { current: { push: (path: string) => void } };
  customHandler: FcmMessageHandler | null;
}

/* ------------------------------------------------------------------ */
/* Foreground Message Handler */
/* ------------------------------------------------------------------ */

export function createForegroundMessageHandler(ctx: ForegroundMessageContext) {
  return (payload: FcmPayload) => {
    // Run custom handlers
    ctx.customHandlers.forEach((handler) => {
      try {
        handler(payload);
      } catch (err) {
        console.error('[FCM] Custom foreground handler failed', err);
      }
    });
  };
}

/* ------------------------------------------------------------------ */
/* Background Message Handler */
/* ------------------------------------------------------------------ */

// Stable reference for SW message handler (module-level to survive remounts)
let onBackgroundMessage: ((event: MessageEvent) => void) | null = null;

export function createBackgroundMessageFactory(ctx: BackgroundMessageContext) {
  return () => {
    if (!onBackgroundMessage) {
      onBackgroundMessage = (event: MessageEvent) => {
        const { type, data } = event.data || {};
        if (type !== 'NOTIFICATION_CLICK') return;

        // Run custom handler
        if (ctx.customHandler) {
          try {
            const payload: FcmPayload = data || {
              from: '',
              collapseKey: '',
              messageId: '',
            };
            ctx.customHandler(payload);
          } catch (err) {
            console.error('[FCM] Custom background handler failed', err);
          }
        }
      };
    }
    return onBackgroundMessage;
  };
}

export function getBackgroundMessageHandler() {
  return onBackgroundMessage;
}

export function clearBackgroundMessageHandler() {
  onBackgroundMessage = null;
}
