import { create } from '@/lib/zustand-reset';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';

export interface LoyaltyLevelDto {
  id: number;
  visitsUpperBound: number;
  isActive: boolean;
}

export interface CompanyDto {
  id: string;
  name: string;
  email: string | null;
  avatarImageUrl: string | null;
  loyaltyLevels: LoyaltyLevelDto[];
}

export interface VenueListItemDto {
  id: string;
  address: string;
  coverImageUrl: string | null;
  isVerified: boolean;
  pinCode: string | null;
  isPinCheckInAllowed: boolean;
}

export interface CompanyDetailDto extends CompanyDto {
  venues: VenueListItemDto[];
}

interface CompaniesState {
  companies: CompanyDto[];
  selectedCompany: CompanyDetailDto | null;
  isLoading: boolean;
  isProfileLoading: boolean;

  fetchCompanies: () => Promise<void>;
  fetchCompany: (id: string) => Promise<void>;
  createCompany: (name: string, email?: string) => Promise<{ id: string }>;
  getAvatarUploadUrl: (
    id: string,
    contentLength: number,
  ) => Promise<{ uploadUrl: string; publicDownloadUrl: string }>;
  patchCompany: (
    id: string,
    patch: { name?: string; email?: string; avatarImageUrl?: string },
  ) => Promise<void>;
  deleteCompany: (id: string) => Promise<void>;
  reset: () => void;
}

export const useCompaniesStore = create<CompaniesState>()(
  persist(
    (set) => ({
      companies: [],
      selectedCompany: null,
      isLoading: true,
      isProfileLoading: false,

      fetchCompanies: async () => {
        set({ isLoading: true });
        try {
          const response = await apiClient.get<{
            companies: CompanyDto[];
          }>(API_ENDPOINTS.B2B.COMPANIES.BASE);
          const { companies } = response.data;

          set({
            companies,
            isLoading: false,
          });
        } catch (error) {
          set({ isLoading: false });
          rethrowApiError(error, 'Failed to fetch companies');
        }
      },
      fetchCompany: async (id: string) => {
        set({ isProfileLoading: true });
        try {
          const response = await apiClient.get<{
            company: CompanyDto;
            venues: VenueListItemDto[];
          }>(API_ENDPOINTS.B2B.COMPANIES.BY_ID(id));
          const { company, venues } = response.data;

          set({
            selectedCompany: { ...company, venues },
            isProfileLoading: false,
          });
        } catch (error) {
          set({ isProfileLoading: false });
          rethrowApiError(error, 'Failed to fetch company');
        }
      },
      createCompany: async (name: string, email?: string) => {
        try {
          const response = await apiClient.post<{ id: string }>(
            API_ENDPOINTS.B2B.COMPANIES.BASE,
            {
              name,
              email,
            },
          );
          return response.data;
        } catch (error) {
          rethrowApiError(error, 'Failed to create company');
        }
      },
      getAvatarUploadUrl: async (id: string, contentLength: number) => {
        try {
          const response = await apiClient.post<{
            uploadUrl: string;
            publicDownloadUrl: string;
          }>(API_ENDPOINTS.B2B.COMPANIES.AVATAR_UPLOAD_URL(id), {
            contentLength,
          });
          return response.data;
        } catch (error) {
          rethrowApiError(error, 'Failed to get upload URL');
        }
      },
      patchCompany: async (id, patch) => {
        try {
          await apiClient.patch<void>(
            API_ENDPOINTS.B2B.COMPANIES.BY_ID(id),
            patch,
          );
          set((state) => ({
            companies: state.companies.map((c) =>
              c.id === id ? { ...c, ...patch } : c,
            ),
            selectedCompany:
              state.selectedCompany?.id === id
                ? { ...state.selectedCompany, ...patch }
                : state.selectedCompany,
          }));
        } catch (error) {
          rethrowApiError(error, 'Failed to update company');
        }
      },
      deleteCompany: async (id: string) => {
        try {
          await apiClient.delete<void>(API_ENDPOINTS.B2B.COMPANIES.BY_ID(id));
          set((state) => ({
            companies: state.companies.filter((c) => c.id !== id),
            selectedCompany:
              state.selectedCompany?.id === id ? null : state.selectedCompany,
          }));
        } catch (error) {
          rethrowApiError(error, 'Failed to delete company');
        }
      },
      reset: () =>
        set({
          companies: [],
          selectedCompany: null,
          isLoading: false,
          isProfileLoading: false,
        }),
    }),
    {
      name: 'b2b-companies-storage',
      partialize: (state) => ({ companies: state.companies }),
    },
  ),
);
