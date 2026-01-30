'use client';

import { PAGE_ROUTES } from '@/lib/routes';
import { FcmPayload } from './types';
import { useChatStore } from '@/store/use-chat-store';

/* ------------------------------------------------------------------ */
/* Types */
/* ------------------------------------------------------------------ */

export interface ForegroundMessageContext {
  router: { push: (path: string) => void };
  isInMeeting: boolean;
  swRegistration: ServiceWorkerRegistration | null;
}

export interface BackgroundMessageContext {
  routerRef: { current: { push: (path: string) => void } };
}

/* ------------------------------------------------------------------ */
/* Foreground Message Handler */
/* ------------------------------------------------------------------ */

export function createForegroundMessageHandler(ctx: ForegroundMessageContext) {
  return (payload: FcmPayload) => {
    const data = payload?.data || {};
    const action = data.action;
    const userAction = data.userAction;
    const notification = payload?.notification;

    // Handle specific actions
    if (action === 'openChatScreen') {
      ctx.router.push(PAGE_ROUTES.CHAT);
    }

    if (action === 'openRateUserScreen') {
      ctx.router.push(PAGE_ROUTES.RATE_USER);
    }

    if (action === 'showEndMeetingSuggestion') {
      // Set flag in chat store - chat page will handle showing the dialog
      useChatStore.getState().setShowEndMeetingPrompt(true);
    }

    // Show system notification for notifications screen if not in meeting
    if (userAction === 'openNotificationsScreen' && !ctx.isInMeeting) {
      if (ctx.swRegistration && notification) {
        ctx.swRegistration.showNotification(
          notification.title || 'Hoppyfy.me',
          {
            body: notification.body || '',
            icon: notification.icon || '/assets/logo.png',
            data: data,
          },
        );
      }
    }
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
        const { type, userAction } = event.data || {};
        if (type !== 'NOTIFICATION_CLICK') return;

        if (userAction === 'openNotificationsScreen') {
          ctx.routerRef.current.push(PAGE_ROUTES.NOTIFICATIONS);
        }
        if (userAction === 'openChatScreen') {
          ctx.routerRef.current.push(PAGE_ROUTES.CHAT);
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
