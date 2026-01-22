'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import axios from 'axios';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface HobbyDto {
  id: string;
  name: string;
}

export interface UserProfileDto {
  id: string;
  firstName: string;
  lastName: string;
  rating: number | null;
  distanceMeters: number;
  coverImageUrl: string;
  hobbies: HobbyDto[];
}

export interface FeedStatusResponse {
  isInMeeting: boolean;
  userIdToRate: string | null;
}

interface FeedState {
  feed: UserProfileDto[];
  currentProfileIndex: number;
  lastFetched: number | null;
  isLoading: boolean;
  error: string | null;
  hasHydrated: boolean;

  fetchFeedStatus: () => Promise<FeedStatusResponse>;
  fetchFeed: () => Promise<void>;
  nextProfile: () => void;
  setHasHydrated: (hydrated: boolean) => void;
  reset: () => void;
}

const CACHE_DURATION_MS = 60 * 60 * 1000; // 1 hour

const initialState = {
  feed: [] as UserProfileDto[],
  currentProfileIndex: 0,
  lastFetched: null as number | null,
  isLoading: false,
  error: null as string | null,
  hasHydrated: false,
};

export const useFeedStore = create<FeedState>()(
  persist(
    (set, get) => ({
      ...initialState,

      fetchFeedStatus: async () => {
        try {
          const response = await axios.get<ApiResult<FeedStatusResponse>>(
            API_ENDPOINTS.FEED.STATUS,
          );

          if (!response.data.success) {
            throw new Error(
              response.data.errors[0]?.error || 'Failed to fetch feed status',
            );
          }

          return response.data.result;
        } catch (error) {
          const errorMessage =
            error instanceof Error
              ? error.message
              : 'Failed to fetch feed status';
          set({ error: errorMessage });
          throw error;
        }
      },

      fetchFeed: async () => {
        const state = get();
        const now = Date.now();

        if (
          state.lastFetched &&
          now - state.lastFetched < CACHE_DURATION_MS &&
          state.feed.length > 0
        ) {
          return;
        }

        set({ isLoading: true, error: null });

        try {
          const response = await axios.get<
            ApiResult<{ feed: UserProfileDto[] }>
          >(API_ENDPOINTS.FEED.LIST);

          if (!response.data.success) {
            throw new Error(
              response.data.errors[0]?.error || 'Failed to fetch feed',
            );
          }

          set({
            feed: response.data.result.feed,
            currentProfileIndex: 0,
            lastFetched: now,
            isLoading: false,
          });
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to fetch feed';
          set({ error: errorMessage, isLoading: false });
        }
      },

      nextProfile: () => {
        const state = get();
        if (state.currentProfileIndex < state.feed.length - 1) {
          set({ currentProfileIndex: state.currentProfileIndex + 1 });
        }
      },

      setHasHydrated: (hydrated) => set({ hasHydrated: hydrated }),
      reset: () => set({ ...initialState, hasHydrated: true }),
    }),
    {
      name: 'feed-store',
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
      partialize: (state) => ({
        feed: state.feed,
        currentProfileIndex: state.currentProfileIndex,
        lastFetched: state.lastFetched,
      }),
    },
  ),
);
