import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { ApiResult } from '@/lib/api-types';
import { PromoType } from '@/lib/promo-types';
import { rethrowApiError } from '@/lib/api-error';

export interface CompanyDto {
  id: string;
  name: string;
  email: string | null;
  avatarImageUrl: string | null;
}

export interface PromoDto {
  type: PromoType;
  body: string;
}

export interface VenueDto {
  id: string;
  qrCode: string;
  isPinCheckInAllowed: boolean;
  latitude: number;
  longitude: number;
  address: string;
  websiteUrl: string | null;
  googleMapsUrl: string;
  instagramUrl: string | null;
  phoneNumber: string | null;
  workingHours: string | null;
  coverImageUrl: string | null;
  promos: PromoDto[];
}

export interface LoyaltyLevelWithDiscountDto {
  userLoyaltyLevel: number;
  visitsCount: number;
  visitsLowerBound: number;
  visitsUpperBound: number | null;
  discount: number | null;
  nextLevelDiscount: number | null;
  isCustomDiscount: boolean;
}

export interface GetVenueResponse {
  company: CompanyDto;
  venue: VenueDto;
  loyaltyLevel: LoyaltyLevelWithDiscountDto;
}

interface VenueState {
  currentVenue: GetVenueResponse | null;
  isLoading: boolean;
  fetchVenue: (id: string) => Promise<void>;
  reset: () => void;
}

export const useVenueStore = create<VenueState>()(
  devtools((set) => ({
    currentVenue: null,
    isLoading: false,
    fetchVenue: async (id: string) => {
      set({ isLoading: true });
      try {
        const response = await apiClient.get<ApiResult<GetVenueResponse>>(
          API_ENDPOINTS.VENUES.DETAILS(id),
        );

        if (response.data.success) {
          set({ currentVenue: response.data.result });
        } else {
          throw new Error('Failed to fetch venue');
        }
      } catch (error) {
        rethrowApiError(error, 'Failed to delete company');
      } finally {
        set({ isLoading: false });
      }
    },
    reset: () => set({ currentVenue: null, isLoading: false }),
  })),
);
