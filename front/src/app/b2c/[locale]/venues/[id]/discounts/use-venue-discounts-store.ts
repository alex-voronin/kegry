import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { ApiResult } from '@/lib/api-types';
import { rethrowApiError } from '@/lib/api-error';

export interface CurrentAndNextLevelDiscountDto {
  currentLevelDiscount: number;
  nextLevelDiscount: number | null;
  isCustomDiscount: boolean;
}

export interface GetDiscountsByWeekdayResponse {
  sunday: CurrentAndNextLevelDiscountDto;
  monday: CurrentAndNextLevelDiscountDto;
  tuesday: CurrentAndNextLevelDiscountDto;
  wednesday: CurrentAndNextLevelDiscountDto;
  thursday: CurrentAndNextLevelDiscountDto;
  friday: CurrentAndNextLevelDiscountDto;
  saturday: CurrentAndNextLevelDiscountDto;
}

interface VenueDiscountsState {
  discounts: GetDiscountsByWeekdayResponse | null;
  isLoading: boolean;
  fetchDiscounts: (venueId: string) => Promise<void>;
  reset: () => void;
}

export const useVenueDiscountsStore = create<VenueDiscountsState>()((set) => ({
  discounts: null,
  isLoading: false,
  fetchDiscounts: async (venueId: string) => {
    set({ discounts: null, isLoading: true });

    try {
      const response = await apiClient.get<
        ApiResult<GetDiscountsByWeekdayResponse>
      >(API_ENDPOINTS.VENUES.DISCOUNTS(venueId));
      set({ discounts: response.data.result });
    } catch (error) {
      rethrowApiError(error, 'Failed to fetch discounts');
    } finally {
      set({ isLoading: false });
    }
  },
  reset: () => set({ discounts: null, isLoading: false }),
}));
