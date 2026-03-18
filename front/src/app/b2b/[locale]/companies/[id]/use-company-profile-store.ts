import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { CompanyDto } from '../use-companies-store';

export interface VenueListItemDto {
  id: string;
  address: string;
  coverImageUrl: string | null;
}

export interface CompanyDetailDto extends CompanyDto {
  venues: VenueListItemDto[];
}

interface CompanyProfileState {
  selectedCompany: CompanyDetailDto | null;
  isLoading: boolean;
  fetchCompany: (id: string) => Promise<void>;
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
      throw error;
    }
  },
  reset: () => set({ selectedCompany: null, isLoading: false }),
}));
