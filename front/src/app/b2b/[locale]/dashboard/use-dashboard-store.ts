import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';

export interface VisitsChartDataPoint {
  label: string;
  count: number;
}

export interface VenueVisitsData {
  venueId: string;
  address: string;
  dataPoints: VisitsChartDataPoint[];
}

export type VisitsChartMode = 'Daily' | 'Weekly' | 'Monthly';

interface DashboardState {
  mode: VisitsChartMode;
  venues: VenueVisitsData[];
  isLoading: boolean;
  hasError: boolean;

  setMode: (mode: VisitsChartMode) => void;
  fetchVisits: (companyId: string, mode: VisitsChartMode) => Promise<void>;
  reset: () => void;
}

export const useDashboardStore = create<DashboardState>()((set) => ({
  mode: 'Daily',
  venues: [],
  isLoading: false,
  hasError: false,

  setMode: (mode) => set({ mode }),

  fetchVisits: async (companyId, mode) => {
    set({ isLoading: true, hasError: false });
    try {
      const response = await apiClient.get(
        API_ENDPOINTS.B2B.DASHBOARD.VISITS,
        { params: { companyId, mode } },
      );
      const { venues } = response.data.result;
      set({ venues, isLoading: false });
    } catch {
      set({ hasError: true, isLoading: false });
    }
  },

  reset: () =>
    set({ mode: 'Daily', venues: [], isLoading: false, hasError: false }),
}));
