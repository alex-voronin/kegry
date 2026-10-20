'use client';

import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult, PaginatedResponse } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface MyEventItem {
  id: string;
  coverImageUrl: string | null;
  title: string;
  type: 'Event' | 'Meetup';
  startDateTime: string;
  venueId: string;
  participantsCount: number;
  maxParticipants: number | null;
  participationStatus?: string;
  creatorId: string;
  creatorAvatarUrl?: string | null;
}

export interface VenueInfo {
  id: string;
  name: string;
  avatarUrl: string | null;
  address: string;
}

type TabType = 'owner' | 'participant';

const DEFAULT_LIMIT = 20;

interface TabState {
  events: MyEventItem[];
  totalCount: number;
  isLoading: boolean;
  hasMore: boolean;
  hasError: boolean;
}

interface MyEventsState {
  owner: TabState;
  participant: TabState;
  venueCache: Record<string, VenueInfo>;

  fetchEvents: (tab: TabType, offset?: number, limit?: number) => Promise<void>;
  loadVenues: (venueIds: string[]) => Promise<void>;
  removeEvent: (tab: TabType, eventId: string) => void;
  reset: () => void;
}

const initialTabState: TabState = {
  events: [],
  totalCount: 0,
  isLoading: false,
  hasMore: true,
  hasError: false,
};

const initialState = {
  owner: { ...initialTabState },
  participant: { ...initialTabState },
  venueCache: {} as Record<string, VenueInfo>,
};

export const useMyEventsStore = create<MyEventsState>()((set, get) => ({
  ...initialState,

  fetchEvents: async (tab, offset = 0, limit = DEFAULT_LIMIT) => {
    set((state) => ({
      [tab]: { ...state[tab], isLoading: true, hasError: false },
    }));

    try {
      const endpoint =
        tab === 'owner'
          ? API_ENDPOINTS.EVENTS.MY_CREATED
          : API_ENDPOINTS.EVENTS.MY_PARTICIPATED;

      const response = await apiClient.get<
        ApiResult<PaginatedResponse<MyEventItem>>
      >(`${endpoint}?offset=${offset}&limit=${limit}`);

      const { data, totalCount } = response.data.result;

      set((state) => {
        const current = state[tab];
        const merged = offset === 0 ? data : [...current.events, ...data];

        return {
          [tab]: {
            events: merged,
            totalCount,
            hasMore: merged.length < totalCount,
            isLoading: false,
            hasError: false,
          },
        };
      });

      // Load venues for uncached IDs
      const allEvents = get()[tab].events;
      const uncachedIds = allEvents
        .map((e) => e.venueId)
        .filter((id) => !get().venueCache[id]);
      const uniqueIds = [...new Set(uncachedIds)];

      if (uniqueIds.length > 0) {
        await get().loadVenues(uniqueIds);
      }
    } catch (error) {
      set((state) => ({
        [tab]: { ...state[tab], isLoading: false, hasError: true },
      }));
      rethrowApiError(error, 'Failed to fetch events');
    }
  },

  loadVenues: async (venueIds) => {
    try {
      const response = await apiClient.post<ApiResult<VenueInfo[]>>(
        API_ENDPOINTS.VENUES.BATCH,
        venueIds,
      );

      const venues = response.data.result;
      const cache: Record<string, VenueInfo> = {};
      for (const v of venues) {
        cache[v.id] = v;
      }

      set((state) => ({
        venueCache: { ...state.venueCache, ...cache },
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to load venues');
    }
  },

  removeEvent: (tab, eventId) => {
    set((state) => ({
      [tab]: {
        ...state[tab],
        events: state[tab].events.filter((e) => e.id !== eventId),
      },
    }));
  },

  reset: () => set({ ...initialState }),
}));
