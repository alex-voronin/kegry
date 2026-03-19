import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';
import { ApiResult } from '@/lib/api-types';

export interface PromoDto {
  body: string;
}

export interface DiscountDto {
  id: string;
  venueId: string;
  loyaltyLevelId: number | null;
  percent: number;
  days: string[];
}

export interface VenueDto {
  id: string;
  latitude: number;
  longitude: number;
  address: string;
  websiteUrl: string | null;
  googleMapsUrl: string | null;
  instagramUrl: string | null;
  phoneNumber: string | null;
  workingHours: string | null;
  coverImageUrl: string | null;
  promos: PromoDto[];
  discounts: DiscountDto[];
}

export interface GetVenueResponse {
  venue: VenueDto;
}

interface VenueState {
  currentVenue: VenueDto | null;
  isLoading: boolean;
  fetchVenue: (id: string) => Promise<void>;
  reset: () => void;
}

export const useVenueStore = create<VenueState>()(
  devtools((set) => ({
    currentVenue: null,
    isLoading: false,
    fetchVenue: async (id: string) => {
      set({ isLoading: true });
      try {
        const response = await apiClient.get<ApiResult<GetVenueResponse>>(
          API_ENDPOINTS.VENUE(id),
        );
        set({ currentVenue: response.data.result.venue });
      } catch (error) {
        rethrowApiError(error, 'Failed to fetch venue');
      } finally {
        set({ isLoading: false });
      }
    },
    reset: () => set({ currentVenue: null, isLoading: false }),
  })),
);
