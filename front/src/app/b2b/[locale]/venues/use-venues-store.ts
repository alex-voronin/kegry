import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';
import { VenueListItemDto } from '../companies/use-companies-store';

export interface GetVenuesResponse {
  venues: VenueListItemDto[];
}

interface VenuesState {
  venues: VenueListItemDto[];
  isLoading: boolean;
  fetchVenues: () => Promise<void>;
  reset: () => void;
}

export const useVenuesStore = create<VenuesState>()((set) => ({
  venues: [],
  isLoading: true,
  fetchVenues: async () => {
    set({ isLoading: true });
    try {
      const response = await apiClient.get<GetVenuesResponse>(
        API_ENDPOINTS.B2B.VENUES.BASE,
      );
      set({ venues: response.data.venues, isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      rethrowApiError(error, 'Failed to fetch venues');
    }
  },
  reset: () => set({ venues: [], isLoading: false }),
}));
