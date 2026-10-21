import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { paramsForReq } from '@/lib/query-params';

export interface VenueListItemDto {
  id: string;
  companyName: string;
  companyAvatarImageUrl: string;
  address: string;
  distanceMeters: number;
  googleMapsUrl: string;
  tagIds: string[];
}

interface VenuesState {
  venues: VenueListItemDto[];
  totalCount: number;
  isLoading: boolean;
  hasMore: boolean;
  hasError: boolean;
  fetchVenues: (
    lat: number,
    lon: number,
    offset: number,
    limit: number,
    tagIds?: string[],
    companyName?: string,
  ) => Promise<void>;
  reset: () => void;
}

export const useVenuesStore = create<VenuesState>()((set) => ({
  venues: [],
  totalCount: 0,
  isLoading: false,
  hasMore: true,
  hasError: false,
  fetchVenues: async (lat, lon, offset, limit, tagIds?, companyName?) => {
    set({ isLoading: true, hasError: false });
    try {
      const params: Record<
        string,
        | string
        | number
        | boolean
        | (string | number | boolean)[]
        | undefined
        | null
      > = {
        latitude: lat,
        longitude: lon,
        offset,
        limit,
      };
      if (tagIds?.length) params['Filter.TagIds'] = tagIds;
      if (companyName) params['Filter.CompanyName'] = companyName;

      const response = await apiClient.get(
        API_ENDPOINTS.VENUES.LIST,
        paramsForReq(params),
      );
      const { data, totalCount } = response.data;

      set((state) => ({
        venues: offset === 0 ? data : [...state.venues, ...data],
        totalCount,
        hasMore:
          (offset === 0 ? data.length : state.venues.length + data.length) <
          totalCount,
        isLoading: false,
      }));
    } catch (error) {
      set({ isLoading: false, hasError: true });
      throw error;
    }
  },
  reset: () =>
    set({ venues: [], totalCount: 0, isLoading: false, hasMore: true }),
}));
