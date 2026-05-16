import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';
import { ApiResult } from '@/lib/api-types';
import { PromoType } from '@/lib/promo-types';

export interface TagDto {
  id: string;
  value: string;
}

export interface PromoDto {
  id: string;
  type: PromoType;
  body: string;
}

export interface DiscountDto {
  id: string;
  venueId: string;
  loyaltyLevelId: number | null;
  percent: number | null;
  days: string[];
}

export interface VenueDto {
  id: string;
  latitude: number;
  longitude: number;
  address: string;
  websiteUrl: string | null;
  googleMapsUrl: string | null;
  instagramUrl: string | null;
  phoneNumber: string | null;
  workingHours: string | null;
  coverImageUrl: string | null;
  isVerified: boolean;
  qrCode: string;
  pinCode: string;
  isGeneratedPinCode: boolean;
  isPinCheckInAllowed: boolean;
  tags: TagDto[];
  promos: PromoDto[];
  discounts: DiscountDto[];
  loyaltyLevels: { id: number; visitsUpperBound: number; isActive: boolean }[];
}

export interface GetVenueResponse {
  venue: VenueDto;
}

interface VenueState {
  currentVenue: VenueDto | null;
  isLoading: boolean;
  fetchVenue: (id: string) => Promise<void>;
  createPromotion: (venueId: string, type: PromoType, body: string) => Promise<void>;
  updatePromotion: (
    venueId: string,
    promoId: string,
    type: PromoType,
    body: string,
  ) => Promise<void>;
  deletePromotion: (venueId: string, promoId: string) => Promise<void>;
  createDiscount: (
    venueId: string,
    percent: number | null,
    days: string[],
    loyaltyLevelId?: number | null,
  ) => Promise<void>;
  updateDiscount: (
    venueId: string,
    discountId: string,
    percent: number | null,
    days: string[],
    loyaltyLevelId?: number | null,
  ) => Promise<void>;
  deleteDiscount: (venueId: string, discountId: string) => Promise<void>;
  deleteVenue: (id: string) => Promise<void>;
  patchVenue: (id: string, request: PatchVenueRequest) => Promise<void>;
  createVenue: (
    companyId: string,
    request: CreateVenueRequest,
  ) => Promise<{ id: string }>;
  reset: () => void;
}

export interface CreateVenueRequest {
  latitude: number;
  longitude: number;
  address: string;
  websiteUrl?: string;
  googleMapsUrl?: string;
  instagramUrl?: string;
  phoneNumber?: string;
  workingHours?: string;
  coverImageUrl?: string;
  tagIds: string[];
}

export interface PatchVenueRequest {
  latitude?: number;
  longitude?: number;
  address?: string;
  websiteUrl?: string;
  googleMapsUrl?: string;
  instagramUrl?: string;
  phoneNumber?: string;
  workingHours?: string;
  coverImageUrl?: string;
  qrCode?: string;
  pinCode?: string;
  isPinCheckInAllowed?: boolean;
  tagIds?: string[];
}

export const useVenueStore = create<VenueState>()((set, get) => ({
  currentVenue: null,
  isLoading: false,
  fetchVenue: async (id: string) => {
    set({ isLoading: true });
    try {
      const response = await apiClient.get<ApiResult<GetVenueResponse>>(
        API_ENDPOINTS.B2B.VENUES.DETAILS(id),
      );
      set({ currentVenue: response.data.result.venue });
    } catch (error) {
      rethrowApiError(error, 'Failed to fetch venue');
    } finally {
      set({ isLoading: false });
    }
  },
  createPromotion: async (venueId: string, type: PromoType, body: string) => {
    const addNewPromoToStore = (
      venue: VenueDto,
      id: string,
      type: PromoType,
      body: string,
    ): VenueDto => ({
      ...venue,
      promos: [...venue.promos, { id, type, body }],
    });

    try {
      const response = await apiClient.post<ApiResult<{ id: string }>>(
        API_ENDPOINTS.B2B.VENUES.PROMOTIONS(venueId),
        { type, body },
      );
      const newId = response.data.result.id;
      set((state) => ({
        currentVenue: state.currentVenue
          ? addNewPromoToStore(state.currentVenue, newId, type, body)
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to create promotion');
    }
  },
  updatePromotion: async (venueId: string, promoId: string, type: PromoType, body: string) => {
    const updatePromoInStore = (
      venue: VenueDto,
      promoId: string,
      newType: PromoType,
      newBody: string,
    ): VenueDto => ({
      ...venue,
      promos: venue.promos.map((p) =>
        p.id === promoId ? { ...p, type: newType, body: newBody } : p,
      ),
    });

    try {
      await apiClient.put(
        API_ENDPOINTS.B2B.VENUES.PROMOTION(venueId, promoId),
        { type, body },
      );
      set((state) => ({
        currentVenue: state.currentVenue
          ? updatePromoInStore(state.currentVenue, promoId, type, body)
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to update promotion');
    }
  },
  deletePromotion: async (venueId: string, promoId: string) => {
    const filterPromosInStore = (
      venue: VenueDto,
      promoId: string,
    ): VenueDto => ({
      ...venue,
      promos: venue.promos.filter((p) => p.id !== promoId),
    });

    try {
      await apiClient.delete(
        API_ENDPOINTS.B2B.VENUES.PROMOTION(venueId, promoId),
      );
      set((state) => ({
        currentVenue: state.currentVenue
          ? filterPromosInStore(state.currentVenue, promoId)
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to delete promotion');
    }
  },
  createDiscount: async (
    venueId: string,
    percent: number | null,
    days: string[],
    loyaltyLevelId?: number | null,
  ) => {
    const addNewDiscountToStore = (
      venue: VenueDto,
      id: string,
      percent: number | null,
      days: string[],
      loyaltyLevelId: number | null,
    ): VenueDto => ({
      ...venue,
      discounts: [
        ...venue.discounts,
        { id, venueId, percent, days, loyaltyLevelId },
      ],
    });

    try {
      const response = await apiClient.post<ApiResult<{ id: string }>>(
        API_ENDPOINTS.B2B.VENUES.DISCOUNTS(venueId),
        { percent, days, loyaltyLevelId },
      );
      const newId = response.data.result.id;
      set((state) => ({
        currentVenue: state.currentVenue
          ? addNewDiscountToStore(
              state.currentVenue,
              newId,
              percent,
              days,
              loyaltyLevelId ?? null,
            )
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to create discount');
    }
  },
  updateDiscount: async (
    venueId: string,
    discountId: string,
    percent: number | null,
    days: string[],
    loyaltyLevelId?: number | null,
  ) => {
    const updateDiscountInStore = (
      venue: VenueDto,
      discountId: string,
      percent: number | null,
      days: string[],
      loyaltyLevelId: number | null,
    ): VenueDto => ({
      ...venue,
      discounts: venue.discounts.map((d) =>
        d.id === discountId ? { ...d, percent, days, loyaltyLevelId } : d,
      ),
    });

    try {
      await apiClient.put(
        API_ENDPOINTS.B2B.VENUES.DISCOUNT(venueId, discountId),
        {
          percent,
          days,
          loyaltyLevelId,
        },
      );
      set((state) => ({
        currentVenue: state.currentVenue
          ? updateDiscountInStore(
              state.currentVenue,
              discountId,
              percent,
              days,
              loyaltyLevelId ?? null,
            )
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to update discount');
    }
  },
  deleteDiscount: async (venueId: string, discountId: string) => {
    const filterDiscountsInStore = (
      venue: VenueDto,
      discountId: string,
    ): VenueDto => ({
      ...venue,
      discounts: venue.discounts.filter((d) => d.id !== discountId),
    });

    try {
      await apiClient.delete(
        API_ENDPOINTS.B2B.VENUES.DISCOUNT(venueId, discountId),
      );
      set((state) => ({
        currentVenue: state.currentVenue
          ? filterDiscountsInStore(state.currentVenue, discountId)
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to delete discount');
    }
  },
  deleteVenue: async (id: string) => {
    try {
      await apiClient.delete(API_ENDPOINTS.B2B.VENUES.DETAILS(id));
    } catch (error) {
      rethrowApiError(error, 'Failed to delete venue');
    }
  },
  patchVenue: async (id: string, request: PatchVenueRequest) => {
    try {
      await apiClient.patch(API_ENDPOINTS.B2B.VENUES.DETAILS(id), request);
      await get().fetchVenue(id);
    } catch (error) {
      rethrowApiError(error, 'Failed to update venue');
    }
  },
  createVenue: async (companyId: string, request: CreateVenueRequest) => {
    try {
      const response = await apiClient.post<ApiResult<{ id: string }>>(
        API_ENDPOINTS.B2B.COMPANIES.VENUE(companyId),
        request,
      );
      return response.data.result;
    } catch (error) {
      rethrowApiError(error, 'Failed to create venue');
    }
  },
  reset: () => set({ currentVenue: null, isLoading: false }),
}));
