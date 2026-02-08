'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
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
  lastFetchedAt: number | null;
  isLoading: boolean;
  error: string | null;
  hasHydrated: boolean;
  unseenNotificationCount: number;
  isInMeeting: boolean;

  fetchFeedStatus: () => Promise<FeedStatusResponse>;
  fetchFeed: () => Promise<void>;
  refreshFeedIfNeeded: () => Promise<void>;
  inviteUser: (userId: string) => Promise<void>;
  skipUser: (userId: string) => Promise<void>;
  ignoreUser: (userId: string) => Promise<void>;
  nextProfile: () => void;
  setHasHydrated: () => void;
  fetchUnseenNotificationCount: () => Promise<void>;
  setUnseenNotificationCount: (
    countOrFn: number | ((current: number) => number),
  ) => void;
  reset: () => void;
}

const FETCH_COOLDOWN_MS = 30 * 1000;
const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour
const MIN_PROFILES_THRESHOLD = 3;

const initialState = {
  feed: [] as UserProfileDto[],
  lastFetchedAt: null as number | null,
  isLoading: false,
  error: null as string | null,
  hasHydrated: false,
  unseenNotificationCount: 0,
  isInMeeting: false,
};

export const useFeedStore = create<FeedState>()(
  persist(
    (set, get) => ({
      ...initialState,

      fetchFeedStatus: async () => {
        try {
          const response = await apiClient.get<ApiResult<FeedStatusResponse>>(
            API_ENDPOINTS.FEED.STATUS,
          );

          if (!response.data.success) {
            throw new Error(
              response.data.errors[0]?.error || 'Failed to fetch feed status',
            );
          }

          set({ isInMeeting: response.data.result.isInMeeting });
          return response.data.result;
        } catch (error) {
          throw error;
        }
      },

      fetchFeed: async () => {
        const state = get();
        const now = Date.now();
        const isCacheValid =
          state.lastFetchedAt &&
          now - state.lastFetchedAt < CACHE_TTL_MS &&
          state.feed.length >= MIN_PROFILES_THRESHOLD;

        if (isCacheValid) {
          return;
        }

        const hasStaleData = state.feed.length > 0;

        if (!hasStaleData) {
          set({ isLoading: true, error: null });
        }

        try {
          const response = await apiClient.get<
            ApiResult<{ feed: UserProfileDto[] }>
          >(API_ENDPOINTS.FEED.LIST);

          if (!response.data.success) {
            throw new Error(
              response.data.errors[0]?.error || 'Failed to fetch feed',
            );
          }

          const newFeed = response.data.result.feed;

          if (hasStaleData) {
            const currentProfile = state.feed[0];
            const existingIds = new Set([currentProfile?.id]);
            const itemsToAdd = newFeed.filter((p) => !existingIds.has(p.id));

            const mergedFeed = currentProfile
              ? [currentProfile, ...itemsToAdd]
              : newFeed;

            set({
              feed: mergedFeed,
              lastFetchedAt: now,
              isLoading: false,
            });
          } else {
            set({
              feed: newFeed,
              lastFetchedAt: now,
              isLoading: false,
            });
          }
        } catch (error) {
          set({ isLoading: false });
          throw error;
        }
      },

      refreshFeedIfNeeded: async () => {
        const state = get();
        const now = Date.now();

        if (state.feed.length >= MIN_PROFILES_THRESHOLD) {
          return;
        }

        if (
          state.lastFetchedAt &&
          now - state.lastFetchedAt < FETCH_COOLDOWN_MS
        ) {
          return;
        }

        try {
          const response = await apiClient.get<
            ApiResult<{ feed: UserProfileDto[] }>
          >(API_ENDPOINTS.FEED.LIST);

          if (!response.data.success) {
            return;
          }

          const newFeed = response.data.result.feed;
          const existingIds = new Set(state.feed.map((p) => p.id));
          const itemsToAdd = newFeed.filter((p) => !existingIds.has(p.id));

          set({
            feed: [...state.feed, ...itemsToAdd],
            lastFetchedAt: now,
          });
        } catch (error) {
          console.log('Failed to refresh feed', error);
        }
      },

      inviteUser: async (userId: string) => {
        await apiClient.post(API_ENDPOINTS.FEED.INVITE(userId));
        get().nextProfile();
        get().refreshFeedIfNeeded();
      },

      skipUser: async (userId: string) => {
        await apiClient.post(API_ENDPOINTS.FEED.SKIP(userId));
        get().nextProfile();
        get().refreshFeedIfNeeded();
      },

      ignoreUser: async (userId: string) => {
        await apiClient.post(API_ENDPOINTS.FEED.IGNORE(userId));
        get().nextProfile();
        get().refreshFeedIfNeeded();
      },

      nextProfile: () => {
        const state = get();
        if (state.feed.length > 1) {
          set({ feed: state.feed.slice(1) });
        } else if (state.feed.length === 1) {
          set({ feed: [] });
        }
      },

      setHasHydrated: () => set({ hasHydrated: true }),

      fetchUnseenNotificationCount: async () => {
        try {
          const response = await apiClient.get<ApiResult<number>>(
            API_ENDPOINTS.NOTIFICATIONS.UNSEEN_COUNT,
          );

          if (response.data.success) {
            set({ unseenNotificationCount: response.data.result });
          } else {
            throw new Error(
              response.data.errors[0]?.error ||
                'Failed to fetch notification count',
            );
          }
        } catch (error) {
          console.error('Failed to fetch unseen notification count:', error);
        }
      },

      setUnseenNotificationCount: (
        countOrFn: number | ((current: number) => number),
      ) =>
        set((state) => ({
          unseenNotificationCount:
            typeof countOrFn === 'function'
              ? countOrFn(state.unseenNotificationCount)
              : countOrFn,
        })),

      reset: () => set({ ...initialState, hasHydrated: true }),
    }),
    {
      name: 'feed-store',
      onRehydrateStorage: () => (state, error) => {
        if (error) {
          console.error('Failed to hydrate feed store:', error);
        }
        state?.setHasHydrated();
      },
      partialize: (state) => ({
        feed: state.feed,
        lastFetchedAt: state.lastFetchedAt,
        isInMeeting: state.isInMeeting,
      }),
    },
  ),
);
