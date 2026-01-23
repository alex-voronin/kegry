'use client';

import { create } from 'zustand';
import axios from 'axios';
import { ApiResult, PaginatedResponse } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface NotificationDto {
  id: string;
  fromUserId: string | null;
  avatarImageUrl: string | null;
  primaryText: string;
  secondaryText: string;
  createdAt: string;
}

interface NotificationsState {
  notifications: NotificationDto[];
  totalCount: number;
  isLoading: boolean;
  error: string | null;
  hasMore: boolean;

  fetchNotifications: (offset?: number, limit?: number) => Promise<void>;
  acceptInvite: (userId: string, notificationId: string) => Promise<void>;
  ignoreInvite: (userId: string, notificationId: string) => Promise<void>;
  blockUser: (userId: string, notificationId: string) => Promise<void>;
  reset: () => void;
}

const DEFAULT_LIMIT = 20;

const initialState = {
  notifications: [] as NotificationDto[],
  totalCount: 0,
  isLoading: false,
  error: null as string | null,
  hasMore: true,
};

export const useNotificationsStore = create<NotificationsState>()(
  (set, get) => ({
    ...initialState,

    fetchNotifications: async (offset = 0, limit = DEFAULT_LIMIT) => {
      set({ isLoading: true, error: null });
      try {
        const response = await axios.get<
          ApiResult<PaginatedResponse<NotificationDto>>
        >(
          `${API_ENDPOINTS.NOTIFICATIONS.LIST}?offset=${offset}&limit=${limit}`,
        );

        if (!response.data.success) {
          throw new Error(
            response.data.errors[0]?.error || 'Failed to fetch notifications',
          );
        }

        const { data, totalCount } = response.data.result;

        if (offset === 0) {
          set({
            notifications: data,
            totalCount,
            hasMore: data.length < totalCount,
            isLoading: false,
          });
        } else {
          set((state) => ({
            notifications: [...state.notifications, ...data],
            totalCount,
            hasMore: state.notifications.length + data.length < totalCount,
            isLoading: false,
          }));
        }
      } catch (error) {
        const errorMessage =
          error instanceof Error
            ? error.message
            : 'Failed to fetch notifications';
        set({ error: errorMessage, isLoading: false });
      }
    },

    acceptInvite: async (userId: string, notificationId: string) => {
      await axios.post(API_ENDPOINTS.FEED.INVITE(userId));
    },

    ignoreInvite: async (userId: string, notificationId: string) => {
      await axios.post(API_ENDPOINTS.FEED.IGNORE(userId));
    },

    blockUser: async (userId: string, notificationId: string) => {
      await axios.post(API_ENDPOINTS.FEED.BLOCK(userId));
    },

    reset: () => set(initialState),
  }),
);

export function isNotificationExpired(createdAt: string): boolean {
  const createdDate = new Date(createdAt);
  const expirationDate = new Date(createdDate.getTime() + 48 * 60 * 60 * 1000);
  return new Date() > expirationDate;
}
