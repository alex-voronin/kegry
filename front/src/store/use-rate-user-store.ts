import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { extractApiError, rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export type Gender = 'male' | 'female';

export interface HobbyDto {
  id: string;
  name: string;
}

export interface UserProfileDto {
  id: string;
  firstName: string;
  lastName: string;
  bio: string;
  avatarImageUrl: string | null;
  coverImageUrl: string | null;
  birthday: string; // ISO date string
  gender: Gender;
  hobbies: HobbyDto[];
}

interface RateUserState {
  userProfile: UserProfileDto | null;
  isLoading: boolean;
  isSubmitting: boolean;

  fetchUserToRate: (userId: string) => Promise<void>;
  submitRating: (userId: string, rating: number | null) => Promise<void>;
  reset: () => void;
}

export const useRateUserStore = create<RateUserState>((set) => ({
  userProfile: null,
  isLoading: false,
  isSubmitting: false,

  fetchUserToRate: async (userId: string) => {
    set({ isLoading: true });
    try {
      const response = await apiClient.get<
        ApiResult<{ userProfile: UserProfileDto }>
      >(`${API_ENDPOINTS.USER_PROFILE.PROFILE}/${userId}`);

      if (response.data.success) {
        set({ userProfile: response.data.result.userProfile });
      }
    } catch (error) {
      console.error(
        'Failed to fetch user profile to rate:',
        extractApiError(error),
      );
    } finally {
      set({ isLoading: false });
    }
  },

  submitRating: async (userId: string, rating: number | null) => {
    set({ isSubmitting: true });
    try {
      await apiClient.post(API_ENDPOINTS.FEED.RATE(userId), {
        rating: rating,
      });
    } catch (error) {
      rethrowApiError(error, 'Failed to submit rating');
    } finally {
      set({ isSubmitting: false });
    }
  },

  reset: () =>
    set({ userProfile: null, isLoading: false, isSubmitting: false }),
}));
