import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { rethrowApiError } from '@/lib/api-error';

export interface EventContact {
  id: string;
  type: string;
  value: string;
  isPublic: boolean;
}

export interface EventDetail {
  id: string;
  coverImageUrl: string | null;
  title: string;
  description: string;
  startDateTime: string;
  type: 'Event' | 'Meetup';
  venueId: string;
  creatorId: string;
  creatorAvatarUrl: string | null;
  isVisibleOnMap: boolean;
  maxParticipants: number | null;
  requiresApproval: boolean;
  participantsCount: number;
  contacts: EventContact[];
  hobbyIds: string[];
  isJoined: boolean;
  isCreator: boolean;
}

export interface VenueInfo {
  address: string;
  name: string;
}

interface MeetupDetailState {
  event: EventDetail | null;
  venue: VenueInfo | null;
  creatorName: string | null;
  creatorHobbies: string[];
  creatorBirthday: string | null;
  isLoading: boolean;
  isJoining: boolean;
  fetchEvent: (eventId: string) => Promise<void>;
  joinEvent: (eventId: string) => Promise<void>;
  leaveEvent: (eventId: string) => Promise<void>;
  reset: () => void;
}

export const useMeetupDetailStore = create<MeetupDetailState>()(
  devtools((set) => ({
    event: null,
    venue: null,
    creatorName: null,
    creatorHobbies: [],
    creatorBirthday: null,
    isLoading: false,
    isJoining: false,

    fetchEvent: async (eventId: string) => {
      set({ isLoading: true });
      try {
        const eventRes = await apiClient.get(API_ENDPOINTS.EVENTS.UPDATE(eventId));
        const ev = eventRes.data.result as EventDetail;
        set({ event: ev });

        const [venueRes] = await Promise.all([
          apiClient.get(API_ENDPOINTS.VENUES.DETAILS(ev.venueId)),
          ev.type === 'Meetup'
            ? apiClient.get(API_ENDPOINTS.USER_PROFILE.PROFILE + '/' + ev.creatorId).then(r => {
                const p = r.data.result.userProfile;
                set({
                  creatorName: p.firstName + ' ' + p.lastName,
                  creatorHobbies: (p.hobbies ?? []).map((h: { id: string; name: string }) => h.name),
                  creatorBirthday: p.birthday ?? null,
                });
              }).catch(() => {})
            : Promise.resolve(),
        ]);
        const venueData = venueRes.data.result;
        set({
          venue: {
            name: venueData.company.name,
            address: venueData.venue.address,
          },
        });
      } catch (error) {
        rethrowApiError(error, 'Failed to load event');
      } finally {
        set({ isLoading: false });
      }
    },

    joinEvent: async (eventId: string) => {
      set({ isJoining: true });
      try {
        await apiClient.post(API_ENDPOINTS.EVENTS.JOIN(eventId));
        set((state) => ({
          event: state.event
            ? { ...state.event, isJoined: true, participantsCount: state.event.participantsCount + 1 }
            : null,
          isJoining: false,
        }));
      } catch {
        set({ isJoining: false });
        throw new Error('Failed to join');
      }
    },

    leaveEvent: async (eventId: string) => {
      set({ isJoining: true });
      try {
        await apiClient.post(API_ENDPOINTS.EVENTS.LEAVE(eventId));
        set((state) => ({
          event: state.event
            ? { ...state.event, isJoined: false, participantsCount: state.event.participantsCount - 1 }
            : null,
          isJoining: false,
        }));
      } catch {
        set({ isJoining: false });
        throw new Error('Failed to leave');
      }
    },

    reset: () =>
      set({
        event: null,
        venue: null,
        creatorName: null,
        creatorHobbies: [],
        creatorBirthday: null,
        isLoading: false,
        isJoining: false,
      }),
  })),
);
