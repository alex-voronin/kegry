'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { handleApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export type Gender = 'male' | 'female';

export interface HobbyDto {
  id: string;
  name: string;
}

export interface UserProfileDto {
  firstName: string;
  lastName: string;
  bio: string;
  avatarImageUrl: string | null;
  coverImageUrl: string | null;
  birthday: string; // ISO date string
  gender: Gender;
  hobbies: HobbyDto[];
}

export interface UserProfileState {
  // Local form state (for onboarding/editing)
  firstName: string;
  lastName: string;
  birthday: string;
  bio: string;
  gender: Gender | null;
  avatarBase64: string | null;
  coverBlob: Blob | null;
  selectedHobbyIds: string[];
  hasHydrated: boolean;

  // API state
  serverProfile: UserProfileDto | null;
  isLoading: boolean;
  error: string | null;

  // Local setters
  setFirstName: (firstName: string) => void;
  setLastName: (lastName: string) => void;
  setBirthday: (birthday: string) => void;
  setBio: (bio: string) => void;
  setGender: (gender: Gender | null) => void;
  setAvatarBase64: (avatar: string | null) => void;
  setCoverBlob: (cover: Blob | null) => void;
  setSelectedHobbyIds: (hobbyIds: string[]) => void;
  setHasHydrated: (hydrated: boolean) => void;

  // API methods
  fetchMyProfile: () => Promise<void>;
  fetchUserProfile: (userId: string) => Promise<UserProfileDto | null>;
  patchProfile: (data: Partial<PatchProfileRequest>) => Promise<void>;

  discardLocalProfileChanges: (serverProfile: UserProfileDto | null) => void;

  reset: () => void;
}

export interface PatchProfileRequest {
  firstName?: string;
  lastName?: string;
  bio?: string;
  birthday?: string;
  gender?: Gender;
  avatarImageUrl?: string;
  coverImageUrl?: string;
  hobbyIds?: string[];
}

const initialState = {
  firstName: '',
  lastName: '',
  birthday: '',
  bio: '',
  gender: null as Gender | null,
  avatarBase64: null as string | null,
  coverBlob: null as Blob | null,
  selectedHobbyIds: [] as string[],
  hasHydrated: false,
  serverProfile: null as UserProfileDto | null,
  isLoading: false,
  error: null as string | null,
};

export const useUserProfileStore = create<UserProfileState>()(
  persist(
    (set, get) => ({
      ...initialState,

      setFirstName: (firstName) => set({ firstName }),
      setLastName: (lastName) => set({ lastName }),
      setBirthday: (birthday) => set({ birthday }),
      setBio: (bio) => set({ bio }),
      setGender: (gender) => set({ gender }),
      setAvatarBase64: (avatarBase64) => set({ avatarBase64 }),
      setCoverBlob: (coverBlob) => set({ coverBlob }),
      setSelectedHobbyIds: (selectedHobbyIds) => set({ selectedHobbyIds }),
      setHasHydrated: (hydrated) => set({ hasHydrated: hydrated }),

      fetchMyProfile: async () => {
        set({ isLoading: true, error: null });
        try {
          const response = await apiClient.get<
            ApiResult<{ userProfile: UserProfileDto }>
          >(API_ENDPOINTS.USER_PROFILE.PROFILE);

          if (!response.data.success) {
            throw new Error(
              response.data.errors[0]?.error || 'Failed to fetch profile',
            );
          }

          const profile = response.data.result.userProfile;
          get().discardLocalProfileChanges(profile);
          set({
            isLoading: false,
          });
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to fetch profile';
          set({ error: errorMessage, isLoading: false });
        }
      },

      fetchUserProfile: async (userId: string) => {
        try {
          const response = await apiClient.get<
            ApiResult<{ userProfile: UserProfileDto }>
          >(`${API_ENDPOINTS.USER_PROFILE.PROFILE}?id=${userId}`);

          if (!response.data.success) {
            throw new Error(
              response.data.errors[0]?.error || 'Failed to fetch user profile',
            );
          }

          return response.data.result.userProfile;
        } catch (error) {
          console.error('Failed to fetch user profile:', error);
          return null;
        }
      },

      patchProfile: async (data: Partial<PatchProfileRequest>) => {
        try {
          const response = await apiClient.patch<ApiResult<void>>(
            API_ENDPOINTS.USER_PROFILE.PROFILE,
            data,
          );

          if (!response.data.success) {
            throw new Error(
              response.data.errors[0]?.error || 'Failed to update profile',
            );
          }

          // Refresh profile after update
          await get().fetchMyProfile();
        } catch (error) {
          handleApiError(error, 'Failed to update profile');
        }
      },

      discardLocalProfileChanges: (
        serverProfile: UserProfileDto | null = null,
      ) => {
        if (serverProfile) {
          set({
            firstName: serverProfile.firstName,
            lastName: serverProfile.lastName,
            birthday: serverProfile.birthday,
            bio: serverProfile.bio,
            gender: serverProfile.gender,
            selectedHobbyIds: serverProfile.hobbies.map((h) => h.id),
          });
        }
      },

      reset: () => set(initialState),
    }),
    {
      name: 'user-profile-store',
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
      partialize: (state) => ({
        firstName: state.firstName,
        lastName: state.lastName,
        birthday: state.birthday,
        bio: state.bio,
        gender: state.gender,
        avatarBase64: state.avatarBase64,
        selectedHobbyIds: state.selectedHobbyIds,
      }),
    },
  ),
);

/**
 * Calculate age from birthday string (ISO date format)
 */
export function calculateAge(birthday: string): number {
  const birthDate = new Date(birthday);
  const today = new Date();
  let age = today.getFullYear() - birthDate.getFullYear();
  const monthDiff = today.getMonth() - birthDate.getMonth();

  if (
    monthDiff < 0 ||
    (monthDiff === 0 && today.getDate() < birthDate.getDate())
  ) {
    age--;
  }

  return age;
}

export function blobToBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result as string);
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
}

export function base64ToBlob(base64: string): Blob {
  const parts = base64.split(',');
  const mimeMatch = parts[0].match(/:(.*?);/);
  const mime = mimeMatch ? mimeMatch[1] : 'image/jpeg';
  const bstr = atob(parts[1]);
  let n = bstr.length;
  const u8arr = new Uint8Array(n);
  while (n--) {
    u8arr[n] = bstr.charCodeAt(n);
  }
  return new Blob([u8arr], { type: mime });
}
