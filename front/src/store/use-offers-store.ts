import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { ApiResult, PaginatedResponse } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface AvailableDiscountDto {
  companyName: string;
  companyAvatarImageUrl: string;
  venueId: string;
  venueAddress: string;
  googleMapsUrl: string | null;
  percent: number;
}

interface OffersState {
  offers: AvailableDiscountDto[];
  totalCount: number;
  isLoading: boolean;
  hasMore: boolean;
  fetchOffers: (
    latitude: number,
    longitude: number,
    offset?: number,
    limit?: number,
  ) => Promise<void>;
  reset: () => void;
}

const DEFAULT_LIMIT = 20;

export const useOffersStore = create<OffersState>()(
  devtools((set) => ({
    offers: [],
    totalCount: 0,
    isLoading: false,
    hasMore: true,

    fetchOffers: async (
      latitude: number,
      longitude: number,
      offset = 0,
      limit = DEFAULT_LIMIT,
    ) => {
      set({ isLoading: true });
      try {
        const response = await apiClient.get<
          ApiResult<PaginatedResponse<AvailableDiscountDto>>
        >(
          `${API_ENDPOINTS.DISCOUNTS.AVAILABLE}?latitude=${latitude}&longitude=${longitude}&offset=${offset}&limit=${limit}`,
        );

        const { data, totalCount } = response.data.result;

        if (offset === 0) {
          set({
            offers: data,
            totalCount,
            hasMore: data.length < totalCount,
            isLoading: false,
          });
        } else {
          set((state) => ({
            offers: [...state.offers, ...data],
            totalCount,
            hasMore: state.offers.length + data.length < totalCount,
            isLoading: false,
          }));
        }
      } catch (error) {
        set({ isLoading: false });
        throw error;
      }
    },

    reset: () => {
      set({
        offers: [],
        totalCount: 0,
        isLoading: false,
        hasMore: true,
      });
    },
  })),
);
