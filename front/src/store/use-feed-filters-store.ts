'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface FilterDto {
  radiusMeters: number;
  ageFrom: number | null;
  ageTo: number | null;
  gender: string | null;
  hobbyIds: string[];
}

export interface GetFilterResponse {
  filter: FilterDto;
}

interface FeedFiltersState {
  filters: FilterDto | null;
  lastFetchedAt: number | null;
  isLoading: boolean;
  hasHydrated: boolean;

  fetchFilters: () => Promise<void>;
  updateFilters: (filters: FilterDto) => Promise<void>;
  setHasHydrated: () => void;
}

const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour

const initialState = {
  filters: null as FilterDto | null,
  lastFetchedAt: null as number | null,
  isLoading: false,
  hasHydrated: false,
};

export const useFeedFiltersStore = create<FeedFiltersState>()(
  persist(
    (set, get) => ({
      ...initialState,

      fetchFilters: async () => {
        const state = get();
        const now = Date.now();
        const isCacheValid =
          state.lastFetchedAt &&
          now - state.lastFetchedAt < CACHE_TTL_MS &&
          state.filters !== null;

        if (isCacheValid) {
          return;
        }

        set({ isLoading: true });

        try {
          const response = await apiClient.get<ApiResult<GetFilterResponse>>(
            API_ENDPOINTS.FEED.FILTER,
          );

          if (!response.data.success) {
            set({ isLoading: false });
            rethrowApiError(
              new Error(response.data.errors[0]?.error),
              'Failed to fetch filters',
            );
          }

          set({
            filters: response.data.result.filter,
            lastFetchedAt: now,
            isLoading: false,
          });
        } catch (error) {
          set({ isLoading: false });
          rethrowApiError(error, 'Failed to fetch filters');
        }
      },

      updateFilters: async (filters: FilterDto) => {
        set({ isLoading: true });
        try {
          const response = await apiClient.put<ApiResult<void>>(
            API_ENDPOINTS.FEED.FILTER,
            { filter: filters },
          );

          if (!response.data.success) {
            set({ isLoading: false });
            rethrowApiError(
              new Error(response.data.errors[0]?.error),
              'Failed to update filters',
            );
          }

          set({ filters, lastFetchedAt: Date.now(), isLoading: false });
        } catch (error) {
          set({ isLoading: false });
          rethrowApiError(error, 'Failed to update filters');
        }
      },

      setHasHydrated: () => set({ hasHydrated: true }),
    }),
    {
      name: 'feed-filters-store',
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated();
      },
      partialize: (state) => ({
        filters: state.filters,
        lastFetchedAt: state.lastFetchedAt,
      }),
    },
  ),
);
