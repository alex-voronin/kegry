'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type Gender = 'male' | 'female';

export interface UserProfileState {
  firstName: string;
  lastName: string;
  birthday: string;
  bio: string;
  gender: Gender | null;
  avatarBlob: Blob | null;
  coverBlob: Blob | null;
  hasHydrated: boolean;

  setFirstName: (firstName: string) => void;
  setLastName: (lastName: string) => void;
  setBirthday: (birthday: string) => void;
  setBio: (bio: string) => void;
  setGender: (gender: Gender | null) => void;
  setAvatarBlob: (avatar: Blob | null) => void;
  setCoverBlob: (cover: Blob | null) => void;
  setHasHydrated: (hydrated: boolean) => void;
  reset: () => void;
}

const initialState = {
  firstName: '',
  lastName: '',
  birthday: '',
  bio: '',
  gender: null as Gender | null,
  avatarBlob: null as Blob | null,
  coverBlob: null as Blob | null,
  hasHydrated: false,
};

export const useUserProfileStore = create<UserProfileState>()(
  persist(
    (set) => ({
      ...initialState,

      setFirstName: (firstName) => set({ firstName }),
      setLastName: (lastName) => set({ lastName }),
      setBirthday: (birthday) => set({ birthday }),
      setBio: (bio) => set({ bio }),
      setGender: (gender) => set({ gender }),
      setAvatarBlob: (avatarBlob) => set({ avatarBlob }),
      setCoverBlob: (coverBlob) => set({ coverBlob }),
      setHasHydrated: (hydrated) => set({ hasHydrated: hydrated }),
      reset: () => set(initialState),
    }),
    {
      name: 'user-profile-store',
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
      // Only persist text fields, NOT blobs (they can't be serialized)
      partialize: (state) => ({
        firstName: state.firstName,
        lastName: state.lastName,
        birthday: state.birthday,
        bio: state.bio,
        gender: state.gender,
      }),
    },
  ),
);
