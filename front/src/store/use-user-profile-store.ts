'use client';

import { create } from '@/lib/zustand-reset';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export type Gender = 'Male' | 'Female';

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
  isPublic: boolean;
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
  isPublic: boolean;
  hasHydrated: boolean;

  // API state
  serverProfile: UserProfileDto | null;
  isLoading: boolean;

  // Local setters
  setFirstName: (firstName: string) => void;
  setLastName: (lastName: string) => void;
  setBirthday: (birthday: string) => void;
  setBio: (bio: string) => void;
  setGender: (gender: Gender | null) => void;
  setAvatarBase64: (avatar: string | null) => void;
  setCoverBlob: (cover: Blob | null) => void;
  setSelectedHobbyIds: (hobbyIds: string[]) => void;
  setIsPublic: (isPublic: boolean) => void;
  setHasHydrated: (hydrated: boolean) => void;

  // API methods
  fetchMyProfile: () => Promise<void>;
  fetchUserProfile: (userId: string) => Promise<UserProfileDto | null>;
  patchProfile: (data: Partial<PatchProfileRequest>) => Promise<void>;
  updatePrivacy: (isPublic: boolean) => Promise<void>;

  discardLocalProfileChanges: (serverProfile: UserProfileDto | null) => void;
  blockUser: (userId: string, reportMessage?: string) => Promise<void>;

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
  isPublic?: boolean;
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
  isPublic: true,
  hasHydrated: false,
  serverProfile: null as UserProfileDto | null,
  isLoading: false,
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
      setIsPublic: (isPublic) => set({ isPublic }),
      setHasHydrated: (hydrated) => set({ hasHydrated: hydrated }),

      fetchMyProfile: async () => {
        set({ isLoading: true });
        try {
          const response = await apiClient.get<
            ApiResult<{ userProfile: UserProfileDto }>
          >(API_ENDPOINTS.USER_PROFILE.PROFILE);

          if (!response.data.success) {
            set({ isLoading: false });
            rethrowApiError(
              new Error(response.data.errors[0]?.error),
              'Failed to fetch profile',
            );
          }

          const profile = response.data.result.userProfile;
          set({ serverProfile: profile });
          get().discardLocalProfileChanges(profile);
          set({ isLoading: false });
        } catch (error) {
          set({ isLoading: false });
          rethrowApiError(error, 'Failed to fetch profile');
        }
      },

      fetchUserProfile: async (userId: string) => {
        const response = await apiClient.get<
          ApiResult<{ userProfile: UserProfileDto }>
        >(`${API_ENDPOINTS.USER_PROFILE.PROFILE}?id=${userId}`);

        if (!response.data.success) {
          rethrowApiError(
            new Error(response.data.errors[0]?.error),
            'Failed to fetch user profile',
          );
        }

        return response.data.result.userProfile;
      },

      patchProfile: async (data: Partial<PatchProfileRequest>) => {
        const response = await apiClient.patch<ApiResult<void>>(
          API_ENDPOINTS.USER_PROFILE.PROFILE,
          data,
        );

        if (!response.data.success) {
          rethrowApiError(
            new Error(response.data.errors[0]?.error),
            'Failed to update profile',
          );
        }

        // Refresh profile after update
        await get().fetchMyProfile();
      },

      updatePrivacy: async (isPublic: boolean) => {
        const response = await apiClient.post<ApiResult<void>>(
          API_ENDPOINTS.USER_PROFILE.PRIVACY,
          { isPublic },
        );

        if (!response.data.success) {
          rethrowApiError(
            new Error(response.data.errors[0]?.error),
            'Failed to update privacy settings',
          );
        }

        set((state) => ({
          isPublic,
          serverProfile: state.serverProfile
            ? {
                ...state.serverProfile,
                isPublic,
              }
            : state.serverProfile,
        }));
      },

      discardLocalProfileChanges: (
        serverProfile: UserProfileDto | null = null,
      ) => {
        const profile = serverProfile || get().serverProfile;
        if (profile) {
          set({
            serverProfile: profile,
            firstName: profile.firstName,
            lastName: profile.lastName,
            birthday: profile.birthday,
            bio: profile.bio,
            gender: profile.gender,
            selectedHobbyIds: profile.hobbies.map((h) => h.id),
            isPublic: profile.isPublic,
          });
        }
      },

      blockUser: async (userId: string, reportMessage?: string) => {
        await apiClient.post(API_ENDPOINTS.FEED.BLOCK(userId), {
          reportMessage,
        });
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
        isPublic: state.isPublic,
      }),
    },
  ),
);

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
