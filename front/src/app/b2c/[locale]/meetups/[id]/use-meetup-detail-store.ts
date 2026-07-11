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
  pendingApprovalsCount: number;
  pendingParticipantIds: string[];
  approvedParticipantIds: string[];
  contacts: EventContact[];
  hobbyIds: string[];
  participationStatus?: string;
  isCreator: boolean;
}

export interface PendingUser {
  id: string;
  firstName: string;
  lastName: string;
  avatarImageUrl: string | null;
  birthday: string | null;
  hobbies: { id: string; name: string }[];
}

export interface VenueInfo {
  address: string;
  name: string;
}

interface MeetupDetailState {
  event: EventDetail | null;
  venue: VenueInfo | null;
  pendingUsers: PendingUser[];
  approvedUsers: PendingUser[];
  isLoading: boolean;
  isJoining: boolean;
  fetchEvent: (eventId: string) => Promise<void>;
  joinEvent: (eventId: string) => Promise<void>;
  leaveEvent: (eventId: string) => Promise<void>;
  fetchPendingUsers: (eventId: string) => Promise<void>;
  fetchApprovedUsers: (eventId: string, extraIds?: string[]) => Promise<void>;
  removePendingUser: (userId: string) => void;
  removeApprovedUser: (userId: string) => void;
  reset: () => void;
}

export const useMeetupDetailStore = create<MeetupDetailState>()(
  devtools((set, get) => ({
    event: null,
    venue: null,
    pendingUsers: [],
    approvedUsers: [],
    isLoading: false,
    isJoining: false,

    fetchEvent: async (eventId: string) => {
      set({ isLoading: true });
      try {
        const eventRes = await apiClient.get(
          API_ENDPOINTS.EVENTS.UPDATE(eventId),
        );
        const ev = eventRes.data.result as EventDetail;
        set({ event: ev });

        const venueRes = await apiClient.get(
          API_ENDPOINTS.VENUES.DETAILS(ev.venueId),
        );
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
        const eventRes = await apiClient.get(
          API_ENDPOINTS.EVENTS.UPDATE(eventId),
        );
        set({ event: eventRes.data.result as EventDetail, isJoining: false });
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
            ? {
                ...state.event,
                participationStatus: undefined,
                participantsCount: Math.max(
                  0,
                  state.event.participantsCount - 1,
                ),
              }
            : null,
          isJoining: false,
        }));
      } catch {
        set({ isJoining: false });
        throw new Error('Failed to leave');
      }
    },

    fetchPendingUsers: async (eventId: string) => {
      const { event } = get();
      if (!event?.pendingParticipantIds?.length) {
        set({ pendingUsers: [] });
        return;
      }
      try {
        const res = await apiClient.post(
          API_ENDPOINTS.USER_PROFILE.PROFILES_BATCH,
          event.pendingParticipantIds,
        );
        set({ pendingUsers: (res.data.result ?? []) as PendingUser[] });
      } catch {
        set({ pendingUsers: [] });
      }
    },

    fetchApprovedUsers: async (eventId: string, extraIds?: string[]) => {
      const { event } = get();
      const ids = [
        ...(event?.approvedParticipantIds ?? []),
        ...(extraIds ?? []),
      ];
      if (!ids.length) {
        set({ approvedUsers: [] });
        return;
      }
      try {
        const res = await apiClient.post(
          API_ENDPOINTS.USER_PROFILE.PROFILES_BATCH,
          ids,
        );
        set({ approvedUsers: (res.data.result ?? []) as PendingUser[] });
      } catch {
        set({ approvedUsers: [] });
      }
    },

    removePendingUser: (userId: string) => {
      set((state) => ({
        pendingUsers: state.pendingUsers.filter((u) => u.id !== userId),
        event: state.event
          ? {
              ...state.event,
              pendingApprovalsCount: Math.max(
                0,
                state.event.pendingApprovalsCount - 1,
              ),
              pendingParticipantIds: state.event.pendingParticipantIds.filter(
                (id) => id !== userId,
              ),
            }
          : null,
      }));
    },

    removeApprovedUser: (userId: string) => {
      set((state) => ({
        approvedUsers: state.approvedUsers.filter((u) => u.id !== userId),
        event: state.event
          ? {
              ...state.event,
              participantsCount: Math.max(0, state.event.participantsCount - 1),
              approvedParticipantIds: state.event.approvedParticipantIds.filter(
                (id) => id !== userId,
              ),
            }
          : null,
      }));
    },

    reset: () =>
      set({
        event: null,
        venue: null,
        pendingUsers: [],
        approvedUsers: [],
        isLoading: false,
        isJoining: false,
      }),
  })),
);
