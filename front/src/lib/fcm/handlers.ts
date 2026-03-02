'use client';

import { FcmPayload, SwMessageType } from './types';
import { useFcmStore } from './store';

/* ------------------------------------------------------------------ */
/* Foreground Message Handler */
/* ------------------------------------------------------------------ */

export function createForegroundMessageHandler() {
  return (payload: FcmPayload) => {
    // Read handler from store at execution time (fixes stale capture bug)
    const handlers = useFcmStore.getState().customForegroundHandlers;
    handlers.forEach((handler) => {
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

export function createSwMessageListener(): (event: MessageEvent) => void {
  return (event: MessageEvent) => {
    const { type, data, payload } = event.data || {};

    if (type === SwMessageType.FCM_MESSAGE) {
      const fcmPayload = payload as FcmPayload;
      if (!fcmPayload) return;

      const handlers = useFcmStore.getState().customForegroundHandlers;
      handlers.forEach((handler) => {
        try {
          handler(fcmPayload);
        } catch (err) {
          console.error('[FCM] SW-to-Foreground bridge failed', err);
        }
      });
      return;
    }

    if (type !== SwMessageType.NOTIFICATION_CLICK) return;

    // Read handler from store at execution time (fixes stale capture bug)
    const handler = useFcmStore.getState().backgroundMessageHandler;
    if (handler) {
      try {
        const payload: FcmPayload = data || {
          from: '',
          collapseKey: '',
          messageId: '',
        };
        handler(payload);
      } catch (err) {
        console.error('[FCM] Custom background handler failed', err);
      }
    }
  };
}
