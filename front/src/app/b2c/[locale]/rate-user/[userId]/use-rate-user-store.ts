import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { extractApiError, rethrowApiError } from '@/lib/api-error';
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

export interface GetUserProfileResponse {
  userProfile: UserProfileDto;
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
      const response = await apiClient.get<GetUserProfileResponse>(
        `${API_ENDPOINTS.USER_PROFILE.PROFILE}/${userId}`,
      );

      set({ userProfile: response.data.userProfile });
    } catch (error) {
      console.error(
        'Failed to fetch user profile to rate:',
        extractApiError(error)[0],
      );
    } finally {
      set({ isLoading: false });
    }
  },

  submitRating: async (userId: string, rating: number | null) => {
    set({ isSubmitting: true });
    try {
      await apiClient.post<void>(API_ENDPOINTS.FEED.RATE(userId), {
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
