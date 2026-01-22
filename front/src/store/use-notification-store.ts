'use client';

import { create } from 'zustand';
import axios from 'axios';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

interface NotificationState {
  unseenCount: number;
  isLoading: boolean;
  error: string | null;

  fetchUnseenCount: () => Promise<void>;
  setUnseenCount: (count: number) => void;
}

export const useNotificationStore = create<NotificationState>()((set) => ({
  unseenCount: 0,
  isLoading: false,
  error: null,

  fetchUnseenCount: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await axios.get<ApiResult<number>>(
        API_ENDPOINTS.NOTIFICATIONS.UNSEEN_COUNT,
      );

      if (response.data.success) {
        set({ unseenCount: response.data.result, isLoading: false });
      } else {
        throw new Error(
          response.data.errors[0]?.error ||
            'Failed to fetch notification count',
        );
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error
          ? error.message
          : 'Failed to fetch notification count';
      set({ error: errorMessage, isLoading: false });
    }
  },

  setUnseenCount: (count) => set({ unseenCount: count }),
}));
