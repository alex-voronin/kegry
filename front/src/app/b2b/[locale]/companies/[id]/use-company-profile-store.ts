import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';
import { CompanyDto } from '../use-companies-store';

export interface VenueListItemDto {
  id: string;
  address: string;
  coverImageUrl: string | null;
  isVerified: boolean;
}

export interface CompanyDetailDto extends CompanyDto {
  venues: VenueListItemDto[];
}

interface CompanyProfileState {
  selectedCompany: CompanyDetailDto | null;
  isLoading: boolean;
  fetchCompany: (id: string) => Promise<void>;
  deleteCompany: (id: string) => Promise<void>;
  reset: () => void;
}

export const useCompanyProfileStore = create<CompanyProfileState>()((set) => ({
  selectedCompany: null,
  isLoading: false,
  fetchCompany: async (id: string) => {
    set({ isLoading: true });
    try {
      const response = await apiClient.get(API_ENDPOINTS.COMPANIES.BY_ID(id));
      const { company, venues } = response.data.result;

      set({
        selectedCompany: { ...company, venues },
        isLoading: false,
      });
    } catch (error) {
      set({ isLoading: false });
      rethrowApiError(error, 'Failed to fetch company');
    }
  },
  deleteCompany: async (id: string) => {
    try {
      await apiClient.delete(API_ENDPOINTS.COMPANIES.BY_ID(id));
    } catch (error) {
      rethrowApiError(error, 'Failed to delete company');
    }
  },
  reset: () => set({ selectedCompany: null, isLoading: false }),
}));
