import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { paramsForReq } from '@/lib/query-params';
import type { VenueMapItem } from '../venues/map/_components/venue-map-internal';
import type { VenueEventsDto } from './_components/types';

type FlyToCoord = { lat: number; lng: number; key: number };

interface MapState {
  venues: VenueMapItem[];
  venueEvents: VenueEventsDto[];
  isLoading: boolean;
  flyToCoord: FlyToCoord | null;
  focusedVenue: VenueMapItem | null;

  fetchMapData: (
    bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number },
    searchQuery: string,
    selectedTagIds: string[],
  ) => Promise<string | null>;
  setFlyToCoord: (coord: FlyToCoord | null) => void;
  setFocusedVenue: (venue: VenueMapItem | null) => void;
  clearFocus: () => void;
}

export const useMapStore = create<MapState>()(
  devtools((set) => ({
    venues: [],
    venueEvents: [],
    isLoading: false,
    flyToCoord: null,
    focusedVenue: null,

    fetchMapData: async (bounds, searchQuery, selectedTagIds) => {
      const filterKey = `${searchQuery}|${selectedTagIds.slice().sort().join(',')}`;
      const boundsKey = `${bounds.minLat.toFixed(5)},${bounds.maxLat.toFixed(5)},${bounds.minLng.toFixed(5)},${bounds.maxLng.toFixed(5)}|${filterKey}`;

      set({ isLoading: true });
      try {
        const venueParams: Record<string, string | number | boolean | (string | number | boolean)[] | undefined | null> = { ...bounds };
        if (selectedTagIds?.length) venueParams['Filter.TagIds'] = selectedTagIds;
        if (searchQuery) venueParams['Filter.CompanyName'] = searchQuery;

        const [venuesRes, eventsRes] = await Promise.all([
          apiClient.get(API_ENDPOINTS.VENUES.MAP, paramsForReq(venueParams)),
          apiClient.get(API_ENDPOINTS.EVENTS.MAP, paramsForReq(bounds)),
        ]);

        set({
          venues: venuesRes.data.result,
          venueEvents: eventsRes.data.result as VenueEventsDto[],
        });
      } finally {
        set({ isLoading: false });
      }

      return boundsKey;
    },

    setFlyToCoord: (coord) => set({ flyToCoord: coord }),
    setFocusedVenue: (venue) => set({ focusedVenue: venue }),
    clearFocus: () => set({ focusedVenue: null }),
  })),
);
