import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { API_ENDPOINTS } from '@/lib/routes';

export interface AccountDto {
  email: string;
}

interface ProfileState {
  account: AccountDto | null;
  isLoading: boolean;
  fetchAccount: () => Promise<void>;
  reset: () => void;
}

export const useProfileStore = create<ProfileState>()((set) => ({
  account: null,
  isLoading: false,
  fetchAccount: async () => {
    set({ isLoading: true });
    try {
      const response = await apiClient.get<AccountDto>(
        API_ENDPOINTS.AUTH.GET_ACCOUNT,
      );
      set({ account: response.data, isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      rethrowApiError(error, 'Failed to fetch account details');
    }
  },
  reset: () => set({ account: null, isLoading: false }),
}));
