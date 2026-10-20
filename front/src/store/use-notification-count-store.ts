'use client';

import { create } from '@/lib/zustand-reset';
import { useEffect } from 'react';
import apiClient from '@/lib/api-client';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';
import { useFcmStore } from '@/lib/fcm/store';
import { NotificationType, FcmPayload } from '@/lib/fcm/types';

export interface NotificationCountState {
  unseenCount: number;
  fetch: () => Promise<void>;
  increment: () => void;
  set: (count: number) => void;
}

const initialState = {
  unseenCount: 0,
};

export const useNotificationCountStore = create<NotificationCountState>()(
  (set) => ({
    ...initialState,

    fetch: async () => {
      try {
        const response = await apiClient.get<ApiResult<number>>(
          API_ENDPOINTS.NOTIFICATIONS.UNSEEN_COUNT,
        );
        set({ unseenCount: response.data.result });
      } catch {
        // Silent failure for notification count
      }
    },

    increment: () => set((state) => ({ unseenCount: state.unseenCount + 1 })),

    set: (count: number) => set({ unseenCount: count }),
  }),
);

export function useNotificationCountInit(enabled = true) {
  const fetch = useNotificationCountStore((s) => s.fetch);
  const increment = useNotificationCountStore((s) => s.increment);

  useEffect(() => {
    if (!enabled) return;

    fetch();

    const { registerForegroundHandler, unregisterForegroundHandler } =
      useFcmStore.getState();

    const handleNotification = (payload: FcmPayload) => {
      if (payload?.data?.type === NotificationType.USER_INVITED) {
        increment();
      }
    };

    registerForegroundHandler(handleNotification);
    return () => unregisterForegroundHandler(handleNotification);
  }, [fetch, increment, enabled]);
}
