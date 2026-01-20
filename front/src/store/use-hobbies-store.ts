'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import axios from 'axios';
import { ApiResult } from '@/lib/api-types';

export interface HobbyOption {
  id: string;
  name: string;
}

interface HobbiesState {
  hobbies: HobbyOption[];
  selectedHobbyIds: string[];
  lastFetched: number | null;
  isLoading: boolean;
  error: string | null;
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

interface GetHobbyListResponse {
  hobbies: ApiHobbyDto[];
}

const initialState = {
  hobbies: [] as HobbyOption[],
  selectedHobbyIds: [] as string[],
  lastFetched: null as number | null,
  isLoading: false,
  error: null as string | null,
  hasHydrated: false,
};

export const useHobbiesStore = create<HobbiesState>()(
  persist(
    (set, get) => ({
      ...initialState,

      fetchHobbies: async () => {
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

        set({ isLoading: true, error: null });

        try {
          const response =
            await axios.get<ApiResult<GetHobbyListResponse>>('/api/v1/hobby');

          if (!response.data.success) {
            throw new Error(
              response.data.errors[0]?.error || 'Failed to fetch hobbies',
            );
          }

          const hobbies = response.data.result.hobbies.map((h) => ({
            id: h.id,
            name: h.name,
          }));

          set({
            hobbies,
            lastFetched: now,
            isLoading: false,
          });
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to fetch hobbies';
          set({ error: errorMessage, isLoading: false });
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
