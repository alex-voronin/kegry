import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';

export interface MetNpcDto {
  id: string;
  name: string;
  avatarUrl: string;
  coverUrl: string;
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
      const response = await apiClient.get<{ metNpcs: MetNpcDto[] }>(
        API_ENDPOINTS.NPC.MET,
      );

      set({
        metNpcs: response.data.metNpcs,
        isLoading: false,
      });
    } catch (error) {
      set({ isLoading: false });
      rethrowApiError(error, 'Failed to fetch met NPCs');
    }
  },

  reset: () => set(initialState),
}));
