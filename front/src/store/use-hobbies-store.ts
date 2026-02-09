'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface HobbyOption {
  id: string;
  name: string;
}

interface HobbiesState {
  hobbies: HobbyOption[];
  selectedHobbyIds: string[];
  lastFetched: number | null;
  isLoading: boolean;
  hasHydrated: boolean;

  fetchHobbies: () => Promise<void>;
  setSelectedHobbyIds: (ids: string[]) => void;
  setHasHydrated: (hydrated: boolean) => void;
  reset: () => void;
}

const CACHE_DURATION_MS = 5 * 60 * 1000; // 5 minutes

interface ApiHobbyDto {
  id: string;
  name: string;
}

interface GetHobbyListResponse {
  hobbies: ApiHobbyDto[];
}

const initialState = {
  hobbies: [] as HobbyOption[],
  selectedHobbyIds: [] as string[],
  lastFetched: null as number | null,
  isLoading: false,
  hasHydrated: false,
};

// Module-level singleton to track if fetch has been initiated this session
let fetchInitiated = false;

export const useHobbiesStore = create<HobbiesState>()(
  persist(
    (set, get) => ({
      ...initialState,

      fetchHobbies: async () => {
        // Singleton check - only allow one fetch per page session
        if (fetchInitiated) {
          return;
        }

        const state = get();
        const now = Date.now();

        // Check if cache is still valid
        if (
          state.lastFetched &&
          now - state.lastFetched < CACHE_DURATION_MS &&
          state.hobbies.length > 0
        ) {
          return;
        }

        // Mark as initiated before async operation
        fetchInitiated = true;

        set({ isLoading: true });

        try {
          const response = await apiClient.get<ApiResult<GetHobbyListResponse>>(
            API_ENDPOINTS.HOBBIES.LIST,
          );

          if (!response.data.success) {
            set({ isLoading: false });
            rethrowApiError(
              new Error(response.data.errors[0]?.error),
              'Failed to fetch hobbies',
            );
          }

          const hobbies = response.data.result.hobbies.map((h) => ({
            id: h.id,
            name: h.name,
          }));

          set({
            hobbies,
            lastFetched: Date.now(),
            isLoading: false,
          });
        } catch (error) {
          set({ isLoading: false });
          rethrowApiError(error, 'Failed to fetch hobbies');
        }
      },

      setSelectedHobbyIds: (ids) => set({ selectedHobbyIds: ids }),
      setHasHydrated: (hydrated) => set({ hasHydrated: hydrated }),
      reset: () =>
        set({
          selectedHobbyIds: [],
        }),
    }),
    {
      name: 'hobbies-store',
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
      partialize: (state) => ({
        hobbies: state.hobbies,
        selectedHobbyIds: state.selectedHobbyIds,
        lastFetched: state.lastFetched,
      }),
    },
  ),
);
