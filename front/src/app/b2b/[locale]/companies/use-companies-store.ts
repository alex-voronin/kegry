import { create } from '@/lib/zustand-reset';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';

export interface CompanyDto {
  id: string;
  name: string;
  email: string | null;
  avatarImageUrl: string | null;
  loyaltyLevels: LoyaltyLevelDto[];
}

export interface LoyaltyLevelDto {
  id: number;
  visitsUpperBound: number;
  isActive: boolean;
}

interface CompaniesState {
  companies: CompanyDto[];
  isLoading: boolean;
  fetchCompanies: () => Promise<void>;
  createCompany: (name: string, email?: string) => Promise<{ id: string }>;
  getAvatarUploadUrl: (
    id: string,
    contentLength: number,
  ) => Promise<{ uploadUrl: string; publicDownloadUrl: string }>;
  patchCompany: (
    id: string,
    patch: { name?: string; email?: string; avatarImageUrl?: string },
  ) => Promise<void>;
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
          const response = await apiClient.get(API_ENDPOINTS.COMPANIES.BASE);
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
      createCompany: async (name: string, email?: string) => {
        const response = await apiClient.post(API_ENDPOINTS.COMPANIES.BASE, {
          name,
          email,
        });
        return response.data.result;
      },
      getAvatarUploadUrl: async (id: string, contentLength: number) => {
        const response = await apiClient.post(
          API_ENDPOINTS.COMPANIES.AVATAR_UPLOAD_URL(id),
          { contentLength },
        );
        return response.data.result;
      },
      patchCompany: async (id, patch) => {
        await apiClient.patch(API_ENDPOINTS.COMPANIES.BY_ID(id), patch);
      },
      reset: () => set({ companies: [], isLoading: false }),
    }),
    {
      name: 'b2b-companies-storage',
      partialize: (state) => ({ companies: state.companies }),
    },
  ),
);
