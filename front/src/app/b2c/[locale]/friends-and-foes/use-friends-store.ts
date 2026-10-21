import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { PaginatedResponse } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';
import { InviteExpiration, computeExpiresAt } from '@/lib/invite-utils';

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
  hasError: boolean;

  fetchFriends: (offset?: number, limit?: number) => Promise<void>;
  removeFriend: (userId: string) => Promise<void>;
  blockFriend: (userId: string, reportMessage?: string) => Promise<void>;
  inviteFriend: (userId: string, when: InviteExpiration) => Promise<void>;
  reset: () => void;
}

const DEFAULT_LIMIT = 20;

const initialState = {
  friends: [] as FriendUserDto[],
  isLoading: false,
  hasMore: true,
  hasError: false,
};

export const useFriendsStore = create<FriendsState>()((set, get) => ({
  ...initialState,

  fetchFriends: async (offset = 0, limit = DEFAULT_LIMIT) => {
    set({ isLoading: true, hasError: false });

    try {
      const response = await apiClient.get<PaginatedResponse<FriendUserDto>>(
        `${API_ENDPOINTS.FEED.FRIENDS}?offset=${offset}&limit=${limit}`,
      );

      const { data, totalCount } = response.data;

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
      set({ isLoading: false, hasError: true });
      rethrowApiError(error, 'Failed to fetch friends');
    }
  },

  removeFriend: async (userId: string) => {
    set({ isLoading: true });
    try {
      await apiClient.post<void>(API_ENDPOINTS.FEED.REMOVE_FRIEND(userId));

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
      await apiClient.post<void>(API_ENDPOINTS.FEED.REMOVE_FRIEND(userId));
      await apiClient.post<void>(API_ENDPOINTS.FEED.BLOCK(userId), {
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

  inviteFriend: async (userId: string, when: InviteExpiration) => {
    await apiClient.post<void>(API_ENDPOINTS.FEED.INVITE_FRIEND(userId), {
      expiresAt: computeExpiresAt(when),
    });
  },

  reset: () => set(initialState),
}));
