import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult, PaginatedResponse } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface FriendUserDto {
  id: string;
  firstName: string;
  lastName: string;
  avatarUrl: string | null;
}

interface FriendsState {
  friends: FriendUserDto[];
  isLoading: boolean;
  hasMore: boolean;

  fetchFriends: (offset?: number, limit?: number) => Promise<void>;
  removeFriend: (userId: string) => Promise<void>;
  blockFriend: (userId: string, reportMessage?: string) => Promise<void>;
  reset: () => void;
}

const DEFAULT_LIMIT = 20;

const initialState = {
  friends: [] as FriendUserDto[],
  isLoading: false,
  hasMore: true,
};

export const useFriendsStore = create<FriendsState>()((set, get) => ({
  ...initialState,

  fetchFriends: async (offset = 0, limit = DEFAULT_LIMIT) => {
    set({ isLoading: true });

    try {
      const response = await apiClient.get<
        ApiResult<PaginatedResponse<FriendUserDto>>
      >(`${API_ENDPOINTS.FEED.FRIENDS}?offset=${offset}&limit=${limit}`);

      if (!response.data.success) {
        set({ isLoading: false });
        rethrowApiError(
          new Error(response.data.errors[0]?.error),
          'Failed to fetch friends',
        );
      }

      const { data, totalCount } = response.data.result;

      if (offset === 0) {
        set({
          friends: data,
          hasMore: data.length < totalCount,
          isLoading: false,
        });
      } else {
        set((state) => ({
          friends: [...state.friends, ...data],
          hasMore: state.friends.length + data.length < totalCount,
          isLoading: false,
        }));
      }
    } catch (error) {
      set({ isLoading: false });
      rethrowApiError(error, 'Failed to fetch friends');
    }
  },

  removeFriend: async (userId: string) => {
    set({ isLoading: true });
    try {
      await apiClient.post(API_ENDPOINTS.FEED.REMOVE_FRIEND(userId));

      set((state) => ({
        friends: state.friends.filter((user) => user.id !== userId),
        isLoading: false,
      }));
    } finally {
      set({ isLoading: false });
    }
  },

  blockFriend: async (userId: string, reportMessage?: string) => {
    set({ isLoading: true });
    try {
      await apiClient.post(API_ENDPOINTS.FEED.REMOVE_FRIEND(userId));
      await apiClient.post(API_ENDPOINTS.FEED.BLOCK(userId), {
        reportMessage: reportMessage || null,
      });

      set((state) => ({
        friends: state.friends.filter((user) => user.id !== userId),
        isLoading: false,
      }));
    } finally {
      set({ isLoading: false });
    }
  },

  reset: () => set(initialState),
}));
