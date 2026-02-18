'use client';

import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { ApiResult } from '@/lib/api-types';
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
      const response = await apiClient.get<ApiResult<{ npc: NpcDto | null }>>(
        API_ENDPOINTS.NPC.GET,
      );

      if (response.data.success) {
        set({ npc: response.data.result.npc, isLoading: false });
        updateRequestSentAt();
      } else {
        set({ isLoading: false });
      }
    } catch {
      set({ isLoading: false });
    }
  },

  inviteNpc: async (id: string) => {
    try {
      const response = await apiClient.get<ApiResult<{ monolog: string }>>(
        API_ENDPOINTS.NPC.INVITE(id),
      );

      if (response.data.success) {
        set({ monolog: response.data.result.monolog });
      }
    } catch (error) {
      console.error('Failed to invite NPC', error);
    }
  },

  dismissNpc: () => set({ npc: null, monolog: null }),

  reset: () => set(initialState),
}));
