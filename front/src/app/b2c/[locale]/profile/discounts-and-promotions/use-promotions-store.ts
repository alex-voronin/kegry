import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { PaginatedResponse } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';
import { PromoType } from '@/lib/promo-types';
import { rethrowApiError } from '@/lib/api-error';

export interface PromoDto {
  type: PromoType;
  body: string;
}

export interface AvailablePromotionDto {
  companyName: string;
  companyAvatarImageUrl: string | null;
  venueId: string;
  venueAddress: string;
  googleMapsUrl: string | null;
  promos: PromoDto[];
}

interface PromotionsState {
  promotions: AvailablePromotionDto[];
  totalCount: number;
  isLoading: boolean;
  hasMore: boolean;
  fetchPromotions: (
    latitude: number,
    longitude: number,
    offset?: number,
    limit?: number,
  ) => Promise<void>;
  reset: () => void;
}

const DEFAULT_LIMIT = 20;

export const usePromotionsStore = create<PromotionsState>()(
  devtools((set) => ({
    promotions: [],
    totalCount: 0,
    isLoading: false,
    hasMore: true,

    fetchPromotions: async (
      latitude: number,
      longitude: number,
      offset = 0,
      limit = DEFAULT_LIMIT,
    ) => {
      set({ isLoading: true });
      try {
        const response = await apiClient.get<
          PaginatedResponse<AvailablePromotionDto>
        >(
          `${API_ENDPOINTS.DEALS.PROMOTIONS}?latitude=${latitude}&longitude=${longitude}&offset=${offset}&limit=${limit}`,
        );

        const { data, totalCount } = response.data;

        if (offset === 0) {
          set({
            promotions: data,
            totalCount,
            hasMore: data.length < totalCount,
            isLoading: false,
          });
        } else {
          set((state) => ({
            promotions: [...state.promotions, ...data],
            totalCount,
            hasMore: state.promotions.length + data.length < totalCount,
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
        promotions: [],
        totalCount: 0,
        isLoading: false,
        hasMore: true,
      });
    },
  })),
);
