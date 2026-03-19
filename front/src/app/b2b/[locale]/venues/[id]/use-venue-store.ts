import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';
import { ApiResult } from '@/lib/api-types';

export interface PromoDto {
  id: string;
  body: string;
}

export interface DiscountDto {
  id: string;
  venueId: string;
  loyaltyLevelId: number | null;
  percent: number;
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
  promos: PromoDto[];
  discounts: DiscountDto[];
}

export interface GetVenueResponse {
  venue: VenueDto;
}

interface VenueState {
  currentVenue: VenueDto | null;
  isLoading: boolean;
  fetchVenue: (id: string) => Promise<void>;
  createPromotion: (venueId: string, body: string) => Promise<void>;
  updatePromotion: (
    venueId: string,
    promoId: string,
    body: string,
  ) => Promise<void>;
  deletePromotion: (venueId: string, promoId: string) => Promise<void>;
  deleteVenue: (id: string) => Promise<void>;
  patchVenue: (id: string, request: PatchVenueRequest) => Promise<void>;
  reset: () => void;
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
}

export const useVenueStore = create<VenueState>()((set, get) => ({
  currentVenue: null,
  isLoading: false,
  fetchVenue: async (id: string) => {
    set({ isLoading: true });
    try {
      const response = await apiClient.get<ApiResult<GetVenueResponse>>(
        API_ENDPOINTS.VENUE(id),
      );
      set({ currentVenue: response.data.result.venue });
    } catch (error) {
      rethrowApiError(error, 'Failed to fetch venue');
    } finally {
      set({ isLoading: false });
    }
  },
  createPromotion: async (venueId: string, body: string) => {
    const addNewPromoToStore = (
      venue: VenueDto,
      id: string,
      body: string,
    ): VenueDto => ({
      ...venue,
      promos: [...venue.promos, { id, body }],
    });

    try {
      const response = await apiClient.post<ApiResult<{ id: string }>>(
        API_ENDPOINTS.VENUE_PROMOTIONS(venueId),
        { body },
      );
      const newId = response.data.result.id;
      set((state) => ({
        currentVenue: state.currentVenue
          ? addNewPromoToStore(state.currentVenue, newId, body)
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to create promotion');
    }
  },
  updatePromotion: async (venueId: string, promoId: string, body: string) => {
    const updatePromoInStore = (
      venue: VenueDto,
      promoId: string,
      newBody: string,
    ): VenueDto => ({
      ...venue,
      promos: venue.promos.map((p) =>
        p.id === promoId ? { ...p, body: newBody } : p,
      ),
    });

    try {
      await apiClient.put(
        `${API_ENDPOINTS.VENUE_PROMOTIONS(venueId)}/${promoId}`,
        { body },
      );
      set((state) => ({
        currentVenue: state.currentVenue
          ? updatePromoInStore(state.currentVenue, promoId, body)
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
      await apiClient.delete(API_ENDPOINTS.VENUE_PROMOTION(venueId, promoId));
      set((state) => ({
        currentVenue: state.currentVenue
          ? filterPromosInStore(state.currentVenue, promoId)
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to delete promotion');
    }
  },
  deleteVenue: async (id: string) => {
    try {
      await apiClient.delete(API_ENDPOINTS.VENUE(id));
    } catch (error) {
      rethrowApiError(error, 'Failed to delete venue');
    }
  },
  patchVenue: async (id: string, request: PatchVenueRequest) => {
    try {
      await apiClient.patch(API_ENDPOINTS.VENUE(id), request);
      set((state) => ({
        currentVenue: state.currentVenue
          ? { ...state.currentVenue, ...request }
          : state.currentVenue,
      }));
    } catch (error) {
      rethrowApiError(error, 'Failed to update venue');
    }
  },
  reset: () => set({ currentVenue: null, isLoading: false }),
}));
