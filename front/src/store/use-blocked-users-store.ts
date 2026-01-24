import { create } from 'zustand';
import axios from 'axios';
import { ApiResult, PaginatedResponse } from '@/lib/api-types';
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

  fetchBlockedUsers: (offset?: number, limit?: number) => Promise<void>;
  unblockUser: (userId: string) => Promise<void>;
  reset: () => void;
}

const DEFAULT_LIMIT = 20;

const initialState = {
  blockedUsers: [] as BlockedUserDto[],
  isLoading: false,
  hasMore: true,
};

export const useBlockedUsersStore = create<BlockedUsersState>()((set, get) => ({
  ...initialState,

  fetchBlockedUsers: async (offset = 0, limit = DEFAULT_LIMIT) => {
    set({ isLoading: true });
    try {
      const response = await axios.get<
        ApiResult<PaginatedResponse<BlockedUserDto>>
      >(`${API_ENDPOINTS.FEED.BLOCKED_USERS}?offset=${offset}&limit=${limit}`);

      if (!response.data.success) {
        throw new Error(
          response.data.errors[0]?.error || 'Failed to fetch blocked users',
        );
      }

      const { data, totalCount } = response.data.result;

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
    } finally {
      set({ isLoading: false });
    }
  },

  unblockUser: async (userId: string) => {
    set({ isLoading: true });
    try {
      await axios.post(API_ENDPOINTS.FEED.UNBLOCK(userId));

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
