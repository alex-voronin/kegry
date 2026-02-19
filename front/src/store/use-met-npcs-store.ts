import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface MetNpcDto {
  id: string;
  name: string;
  avatarUrl: string;
}

interface MetNpcsState {
  metNpcs: MetNpcDto[];
  isLoading: boolean;

  fetchMetNpcs: () => Promise<void>;
  reset: () => void;
}

const initialState = {
  metNpcs: [] as MetNpcDto[],
  isLoading: false,
};

export const useMetNpcsStore = create<MetNpcsState>()((set) => ({
  ...initialState,

  fetchMetNpcs: async () => {
    set({ isLoading: true });

    try {
      const response = await apiClient.get<ApiResult<{ metNpcs: MetNpcDto[] }>>(
        API_ENDPOINTS.NPC.MET,
      );

      if (!response.data.success) {
        set({ isLoading: false });
        rethrowApiError(
          new Error(response.data.errors[0]?.error),
          'Failed to fetch met NPCs',
        );
      }

      set({
        metNpcs: response.data.result.metNpcs,
        isLoading: false,
      });
    } catch (error) {
      set({ isLoading: false });
      rethrowApiError(error, 'Failed to fetch met NPCs');
    }
  },

  reset: () => set(initialState),
}));
