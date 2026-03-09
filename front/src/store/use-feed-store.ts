'use client';

import { create } from '@/lib/zustand-reset';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';
import { computeExpiresAt, InviteExpiration } from '@/lib/invite-utils';

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
  availableInvitesCount: number;
}

interface FeedState {
  feed: UserProfileDto[];
  lastFetchedAt: number | null;
  isLoading: boolean;
  hasHydrated: boolean;
  unseenNotificationCount: number;
  isInMeeting: boolean;
  availableInvitesCount: number;
  isFetching: boolean;
  isPerformingAction: boolean;

  fetchFeedStatus: () => Promise<FeedStatusResponse>;
  fetchFeed: () => Promise<void>;
  refreshFeedIfNeeded: () => Promise<void>;
  inviteUser: (userId: string, when: InviteExpiration) => Promise<void>;
  getInviteExpiration: (userId: string) => Promise<string | null>;
  skipUser: (userId: string) => Promise<void>;
  ignoreUser: (userId: string) => Promise<void>;
  setIsPerformingAction: (isPerformingAction: boolean) => void;
  nextProfile: () => void;
  setHasHydrated: () => void;
  fetchUnseenNotificationCount: () => Promise<void>;
  setUnseenNotificationCount: (
    countOrFn: number | ((current: number) => number),
  ) => void;
  reset: () => void;
}

const FETCH_COOLDOWN_MS = 10 * 1000;
const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour
const MIN_PROFILES_THRESHOLD = 3;

const initialState = {
  feed: [] as UserProfileDto[],
  lastFetchedAt: null as number | null,
  isLoading: false,
  hasHydrated: false,
  unseenNotificationCount: 0,
  isInMeeting: false,
  availableInvitesCount: 0,
  isFetching: false,
  isPerformingAction: false,
};

export const useFeedStore = create<FeedState>()(
  persist(
    (set, get) => ({
      ...initialState,

      fetchFeedStatus: async () => {
        const response = await apiClient.get<ApiResult<FeedStatusResponse>>(
          API_ENDPOINTS.FEED.STATUS,
        );

        set({
          isInMeeting: response.data.result.isInMeeting,
          availableInvitesCount: response.data.result.availableInvitesCount,
        });
        return response.data.result;
      },

      fetchFeed: async () => {
        const state = get();
        const now = Date.now();
        const isCacheValid =
          state.lastFetchedAt &&
          now - state.lastFetchedAt < CACHE_TTL_MS &&
          state.feed.length >= MIN_PROFILES_THRESHOLD;

        if (isCacheValid || state.isFetching) {
          return;
        }

        set({ isFetching: true });

        const hasStaleData = state.feed.length > 0;

        if (!hasStaleData) {
          set({ isLoading: true });
        }

        try {
          const response = await apiClient.get<
            ApiResult<{ feed: UserProfileDto[] }>
          >(API_ENDPOINTS.FEED.LIST);

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
          rethrowApiError(error, 'Failed to fetch feed');
        } finally {
          set({ isFetching: false });
        }
      },

      refreshFeedIfNeeded: async () => {
        const state = get();
        const now = Date.now();

        if (state.feed.length >= MIN_PROFILES_THRESHOLD) {
          return;
        }

        if (
          (state.lastFetchedAt &&
            now - state.lastFetchedAt < FETCH_COOLDOWN_MS) ||
          state.isFetching
        ) {
          return;
        }

        set({ isFetching: true });

        try {
          const response = await apiClient.get<
            ApiResult<{ feed: UserProfileDto[] }>
          >(API_ENDPOINTS.FEED.LIST);

          const newFeed = response.data.result.feed;
          const existingIds = new Set(state.feed.map((p) => p.id));
          const itemsToAdd = newFeed.filter((p) => !existingIds.has(p.id));

          set({
            feed: [...state.feed, ...itemsToAdd],
            lastFetchedAt: now,
          });
        } catch {
          // Silent failure for background refresh
        } finally {
          set({ isFetching: false });
        }
      },

      inviteUser: async (userId: string, when: InviteExpiration) => {
        set({ isPerformingAction: true });
        try {
          await apiClient.post(API_ENDPOINTS.FEED.INVITE(userId), {
            expiresAt: computeExpiresAt(when),
          });
          set((state) => ({
            availableInvitesCount: Math.max(0, state.availableInvitesCount - 1),
          }));
          get().nextProfile();
          get().refreshFeedIfNeeded();
        } finally {
          set({ isPerformingAction: false });
        }
      },

      getInviteExpiration: async (userId: string): Promise<string | null> => {
        try {
          const response = await apiClient.get<
            ApiResult<{ inviteExpiresAt: string | null }>
          >(API_ENDPOINTS.FEED.INVITE_EXPIRATION(userId));
          return response.data.result?.inviteExpiresAt ?? null;
        } catch {
          return null;
        }
      },

      skipUser: async (userId: string) => {
        set({ isPerformingAction: true });
        try {
          await apiClient.post(API_ENDPOINTS.FEED.SKIP(userId));
          get().nextProfile();
          get().refreshFeedIfNeeded();
        } finally {
          set({ isPerformingAction: false });
        }
      },

      ignoreUser: async (userId: string) => {
        set({ isPerformingAction: true });
        try {
          await apiClient.post(API_ENDPOINTS.FEED.IGNORE(userId));
          get().nextProfile();
          get().refreshFeedIfNeeded();
        } finally {
          set({ isPerformingAction: false });
        }
      },

      setIsPerformingAction: (isPerformingAction) =>
        set({ isPerformingAction: isPerformingAction }),

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
          }
        } catch {
          // Silent failure for notification count
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
        availableInvitesCount: state.availableInvitesCount,
      }),
    },
  ),
);
