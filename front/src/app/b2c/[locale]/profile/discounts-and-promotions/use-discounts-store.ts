import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { PaginatedResponse } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';
import { rethrowApiError } from '@/lib/api-error';

export interface AvailableDiscountDto {
  companyName: string;
  companyAvatarImageUrl: string | null;
  venueId: string;
  venueAddress: string;
  googleMapsUrl: string | null;
  percent: number | null;
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

export const useDiscountsStore = create<OffersState>()(
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
          PaginatedResponse<AvailableDiscountDto>
        >(
          `${API_ENDPOINTS.DEALS.DISCOUNTS}?latitude=${latitude}&longitude=${longitude}&offset=${offset}&limit=${limit}`,
        );

        const { data, totalCount } = response.data;

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
        rethrowApiError(error);
      } finally {
        set({ isLoading: false });
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
