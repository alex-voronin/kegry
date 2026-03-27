import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';
import { VenueListItemDto } from '../companies/[id]/use-company-profile-store';

interface VenuesState {
  venues: VenueListItemDto[];
  isLoading: boolean;
  fetchVenues: () => Promise<void>;
  reset: () => void;
}

export const useVenuesStore = create<VenuesState>()((set) => ({
  venues: [],
  isLoading: false,
  fetchVenues: async () => {
    set({ isLoading: true });
    try {
      const response = await apiClient.get<{ result: VenueListItemDto[] }>(
        API_ENDPOINTS.B2B.VENUES.BASE,
      );
      set({ venues: response.data.result, isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      rethrowApiError(error, 'Failed to fetch venues');
    }
  },
  reset: () => set({ venues: [], isLoading: false }),
}));
