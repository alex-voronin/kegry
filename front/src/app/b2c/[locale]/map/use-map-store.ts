import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';
import { paramsForReq } from '@/lib/query-params';
import type { VenueMapItem } from './_components/venue-map-internal';
import type { VenueEventsDto } from './_components/types';

type FlyToCoord = { lat: number; lng: number; key: number };

interface MapState {
  venues: VenueMapItem[];
  venueEvents: VenueEventsDto[];
  isLoading: boolean;
  flyToCoord: FlyToCoord | null;
  focusedVenue: VenueMapItem | null;
  hasActiveChat: boolean;

  fetchMapData: (
    bounds: { minLat: number; maxLat: number; minLng: number; maxLng: number },
    searchQuery: string,
    selectedTagIds: string[],
    eventTypes?: string[],
  ) => Promise<string | null>;
  setFlyToCoord: (coord: FlyToCoord | null) => void;
  flyTo: (lat: number, lng: number, venue?: VenueMapItem | null) => void;
  setFocusedVenue: (venue: VenueMapItem | null) => void;
  clearFocus: () => void;
  updateEventParticipation: (
    eventId: string,
    participationStatus: string | undefined,
  ) => void;
  deleteEvent: (eventId: string) => void;
  fetchChatStatus: () => Promise<void>;
}

export const useMapStore = create<MapState>()(
  devtools((set, get) => ({
    venues: [],
    venueEvents: [],
    isLoading: false,
    flyToCoord: null,
    focusedVenue: null,
    hasActiveChat: false,

    fetchMapData: async (bounds, searchQuery, selectedTagIds, eventTypes) => {
      const filterKey = `${searchQuery}|${selectedTagIds.slice().sort().join(',')}`;
      const boundsKey = `${bounds.minLat.toFixed(5)},${bounds.maxLat.toFixed(5)},${bounds.minLng.toFixed(5)},${bounds.maxLng.toFixed(5)}|${filterKey}`;
      const hasFilters = !!(searchQuery || selectedTagIds?.length);

      const eventsBody: Record<string, unknown> = {
        minLat: bounds.minLat,
        maxLat: bounds.maxLat,
        minLng: bounds.minLng,
        maxLng: bounds.maxLng,
      };
      eventsBody.eventTypes = eventTypes ?? ['Event', 'Meetup'];

      set({ isLoading: true });
      try {
        const venueParams: Record<
          string,
          | string
          | number
          | boolean
          | (string | number | boolean)[]
          | undefined
          | null
        > = { ...bounds };
        if (selectedTagIds?.length)
          venueParams['Filter.TagIds'] = selectedTagIds;
        if (searchQuery) venueParams['Filter.CompanyName'] = searchQuery;

        if (hasFilters) {
          const venuesRes = await apiClient.get<VenueMapItem[]>(
            API_ENDPOINTS.VENUES.MAP,
            paramsForReq(venueParams),
          );
          const venues: VenueMapItem[] = venuesRes.data;
          eventsBody.venueIds = venues.map((v) => v.id);

          const eventsRes = await apiClient.post<VenueEventsDto[]>(
            API_ENDPOINTS.EVENTS.MAP,
            eventsBody,
          );

          set({
            venues,
            venueEvents: eventsRes.data,
          });
        } else {
          const [venuesRes, eventsRes] = await Promise.all([
            apiClient.get<VenueMapItem[]>(
              API_ENDPOINTS.VENUES.MAP,
              paramsForReq(venueParams),
            ),
            apiClient.post<VenueEventsDto[]>(
              API_ENDPOINTS.EVENTS.MAP,
              eventsBody,
            ),
          ]);

          set({
            venues: venuesRes.data,
            venueEvents: eventsRes.data,
          });
        }
      } finally {
        set({ isLoading: false });
      }

      return boundsKey;
    },

    setFlyToCoord: (coord) => set({ flyToCoord: coord }),
    flyTo: (lat, lng, venue) => {
      set((state) => ({
        flyToCoord: { lat, lng, key: (state.flyToCoord?.key ?? 0) + 1 },
        focusedVenue: venue ?? state.focusedVenue,
      }));
    },
    setFocusedVenue: (venue) => set({ focusedVenue: venue }),
    clearFocus: () => set({ focusedVenue: null }),

    updateEventParticipation: (
      eventId: string,
      participationStatus: string | undefined,
    ) => {
      set((state) => ({
        venueEvents: state.venueEvents.map((v) => ({
          ...v,
          events: v.events.map((e) =>
            e.id === eventId ? { ...e, participationStatus } : e,
          ),
        })),
      }));
    },

    fetchChatStatus: async () => {
      try {
        const res = await apiClient.get<{ hasActiveChat: boolean }>(
          API_ENDPOINTS.CHAT.STATUS,
        );
        set({ hasActiveChat: res.data.hasActiveChat });
      } catch {
        set({ hasActiveChat: false });
      }
    },

    deleteEvent: async (eventId: string) => {
      try {
        await apiClient.delete<void>(API_ENDPOINTS.EVENTS.DELETE(eventId));
        set((state) => ({
          venueEvents: state.venueEvents
            .map((v) => ({
              ...v,
              events: v.events.filter((e) => e.id !== eventId),
              totalCount: v.events.filter((e) => e.id !== eventId).length,
            }))
            .filter((v) => v.events.length > 0),
        }));
      } catch (error) {
        rethrowApiError(error, 'Failed to delete event');
      }
    },
  })),
);
