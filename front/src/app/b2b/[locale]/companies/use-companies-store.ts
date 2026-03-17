import { create } from '@/lib/zustand-reset';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';

export interface CompanyDto {
  id: string;
  name: string;
  email: string | null;
  avatarImageUrl: string | null;
}

interface CompaniesState {
  companies: CompanyDto[];
  isLoading: boolean;
  fetchCompanies: () => Promise<void>;
  reset: () => void;
}

export const useCompaniesStore = create<CompaniesState>()(
  persist(
    (set) => ({
      companies: [],
      isLoading: false,
      fetchCompanies: async () => {
        set({ isLoading: true });
        try {
          const response = await apiClient.get(API_ENDPOINTS.COMPANIES);
          const { companies } = response.data.result;

          set({
            companies,
            isLoading: false,
          });
        } catch (error) {
          set({ isLoading: false });
          throw error;
        }
      },
      reset: () => set({ companies: [], isLoading: false }),
    }),
    {
      name: 'b2b-companies-storage',
      partialize: (state) => ({ companies: state.companies }),
    },
  ),
);
