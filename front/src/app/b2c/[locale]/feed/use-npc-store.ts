'use client';

import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { useFeedStore } from './use-feed-store';
import { API_ENDPOINTS } from '@/lib/routes';

// --- 1-hour cooldown for NPC requests ---
const NPC_COOLDOWN_MS = 60 * 60 * 1000; // 1 hour
const REQUEST_SENT_AT_KEY = 'npc_request_sent_at';

function isNpcOnCooldown(): boolean {
  const raw = localStorage.getItem(REQUEST_SENT_AT_KEY);
  if (!raw) return false;
  return Date.now() - Number(raw) < NPC_COOLDOWN_MS;
}

function updateRequestSentAt(): void {
  localStorage.setItem(REQUEST_SENT_AT_KEY, String(Date.now()));
}

export interface NpcDto {
  id: string;
  name: string;
  avatarUrl: string;
  coverUrl: string;
}

interface NpcState {
  npc: NpcDto | null;
  monolog: string | null;
  isLoading: boolean;

  fetchNpc: () => Promise<void>;
  inviteNpc: (id: string) => Promise<void>;
  dismissNpc: () => void;
  reset: () => void;
}

const initialState = {
  npc: null as NpcDto | null,
  monolog: null as string | null,
  isLoading: false,
};

export const useNpcStore = create<NpcState>()((set) => ({
  ...initialState,

  fetchNpc: async () => {
    if (isNpcOnCooldown()) {
      return;
    }

    set({ isLoading: true });
    try {
      const response = await apiClient.get<{ npc: NpcDto | null }>(
        API_ENDPOINTS.NPC.GET,
      );

      set({ npc: response.data.npc, isLoading: false });
      updateRequestSentAt();
    } catch {
      set({ isLoading: false });
    }
  },

  inviteNpc: async (id: string) => {
    useFeedStore.getState().setIsPerformingAction(true);
    try {
      const response = await apiClient.get<{ monolog: string }>(
        API_ENDPOINTS.NPC.INVITE(id),
      );

      set({ monolog: response.data.monolog });
    } catch (error) {
      console.error('Failed to invite NPC', error);
    } finally {
      useFeedStore.getState().setIsPerformingAction(false);
    }
  },

  dismissNpc: () => {
    useFeedStore.getState().setIsPerformingAction(true);
    set({ npc: null, monolog: null });
    useFeedStore.getState().setIsPerformingAction(false);
  },

  reset: () => set(initialState),
}));
