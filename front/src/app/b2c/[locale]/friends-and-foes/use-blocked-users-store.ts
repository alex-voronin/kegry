import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { PaginatedResponse } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface BlockedUserDto {
  id: string;
  firstName: string;
  lastName: string;
  avatarUrl: string | null;
}

interface BlockedUsersState {
  blockedUsers: BlockedUserDto[];
  isLoading: boolean;
  hasMore: boolean;
  hasError: boolean;

  fetchBlockedUsers: (offset?: number, limit?: number) => Promise<void>;
  unblockUser: (userId: string) => Promise<void>;
  reset: () => void;
}

const DEFAULT_LIMIT = 20;

const initialState = {
  blockedUsers: [] as BlockedUserDto[],
  isLoading: false,
  hasMore: true,
  hasError: false,
};

export const useBlockedUsersStore = create<BlockedUsersState>()((set, get) => ({
  ...initialState,

  fetchBlockedUsers: async (offset = 0, limit = DEFAULT_LIMIT) => {
    set({ isLoading: true, hasError: false });
    try {
      const response = await apiClient.get<PaginatedResponse<BlockedUserDto>>(
        `${API_ENDPOINTS.FEED.BLOCKED_USERS}?offset=${offset}&limit=${limit}`,
      );

      const { data, totalCount } = response.data;

      if (offset === 0) {
        set({
          blockedUsers: data,
          hasMore: data.length < totalCount,
          isLoading: false,
        });
      } else {
        set((state) => ({
          blockedUsers: [...state.blockedUsers, ...data],
          hasMore: state.blockedUsers.length + data.length < totalCount,
          isLoading: false,
        }));
      }
    } catch (error) {
      set({ isLoading: false, hasError: true });
      rethrowApiError(error, 'Failed to fetch blocked users');
    }
  },

  unblockUser: async (userId: string) => {
    set({ isLoading: true });
    try {
      await apiClient.post<void>(API_ENDPOINTS.FEED.UNBLOCK(userId));

      // Optimistic update
      set((state) => ({
        blockedUsers: state.blockedUsers.filter((user) => user.id !== userId),
        isLoading: false,
      }));
    } finally {
      set({ isLoading: false });
    }
  },

  reset: () => set(initialState),
}));
