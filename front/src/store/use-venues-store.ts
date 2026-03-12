import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';

export interface VenueListItemDto {
  id: string;
  companyName: string;
  companyAvatarImageUrl: string;
  address: string;
  distanceMeters: number;
  googleMapsUrl: string;
}

interface VenuesState {
  venues: VenueListItemDto[];
  totalCount: number;
  isLoading: boolean;
  hasMore: boolean;
  fetchVenues: (
    lat: number,
    lon: number,
    offset: number,
    limit: number,
  ) => Promise<void>;
  reset: () => void;
}

export const useVenuesStore = create<VenuesState>()((set) => ({
  venues: [],
  totalCount: 0,
  isLoading: false,
  hasMore: true,
  fetchVenues: async (lat, lon, offset, limit) => {
    set({ isLoading: true });
    try {
      const response = await apiClient.post(API_ENDPOINTS.VENUES, {
        latitude: lat,
        longitude: lon,
        offset,
        limit,
      });
      const { data, totalCount } = response.data.result;

      set((state) => ({
        venues: offset === 0 ? data : [...state.venues, ...data],
        totalCount,
        hasMore:
          (offset === 0 ? data.length : state.venues.length + data.length) <
          totalCount,
        isLoading: false,
      }));
    } catch (error) {
      throw error;
    } finally {
      set({ isLoading: false });
    }
  },
  reset: () =>
    set({ venues: [], totalCount: 0, isLoading: false, hasMore: true }),
}));
