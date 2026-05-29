import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';

export interface B2BEventListItemDto {
  id: string;
  coverImageUrl: string | null;
  title: string;
  type: 'Event' | 'Meetup';
  startDateTime: string;
  venueId: string;
  venueAddress: string;
  isVisibleOnMap: boolean;
  maxParticipants: number | null;
  requiresApproval: boolean;
  participantsCount: number;
}

interface EventsState {
  events: B2BEventListItemDto[];
  isLoading: boolean;
  fetchEvents: (venueId?: string) => Promise<void>;
  reset: () => void;
}

export const useEventsStore = create<EventsState>()((set) => ({
  events: [],
  isLoading: true,
  fetchEvents: async (venueId?: string) => {
    set({ isLoading: true });
    try {
      const params = venueId ? { venueId } : {};
      const response = await apiClient.get<{
        result: { data: B2BEventListItemDto[]; totalCount: number };
      }>(API_ENDPOINTS.B2B.EVENTS.BASE, { params });
      set({ events: response.data.result.data, isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      rethrowApiError(error, 'Failed to fetch events');
    }
  },
  reset: () => set({ events: [], isLoading: false }),
}));
