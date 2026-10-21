'use client';

import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';

const THROTTLE_MS = 10_000;

interface PendingApprovalsState {
  total: number;
  isLoading: boolean;
  lastFetchedAt: number | null;
  fetch: () => Promise<void>;
}

export const usePendingApprovalsStore = create<PendingApprovalsState>(
  (set, get) => ({
    total: 0,
    isLoading: false,
    lastFetchedAt: null,

    fetch: async () => {
      const { lastFetchedAt } = get();
      if (lastFetchedAt && Date.now() - lastFetchedAt < THROTTLE_MS) return;

      set({ isLoading: true });
      try {
        const res = await apiClient.get<{ total: number }>(
          API_ENDPOINTS.EVENTS.PENDING_COUNT,
        );
        set({ total: res.data.total, lastFetchedAt: Date.now() });
      } catch {
        // silently fail
      } finally {
        set({ isLoading: false });
      }
    },
  }),
);
