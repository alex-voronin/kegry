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
  avatarBase64: string | null; // Base64 for localStorage persistence
  coverBlob: Blob | null; // Blob for memory only (cover page)
  hasHydrated: boolean;

  setFirstName: (firstName: string) => void;
  setLastName: (lastName: string) => void;
  setBirthday: (birthday: string) => void;
  setBio: (bio: string) => void;
  setGender: (gender: Gender | null) => void;
  setAvatarBase64: (avatar: string | null) => void;
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
  avatarBase64: null as string | null,
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
      setAvatarBase64: (avatarBase64) => set({ avatarBase64 }),
      setCoverBlob: (coverBlob) => set({ coverBlob }),
      setHasHydrated: (hydrated) => set({ hasHydrated: hydrated }),
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
        avatarBase64: state.avatarBase64, // Now persisted!
      }),
    },
  ),
);

/**
 * Convert a Blob to base64 string
 */
export function blobToBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result as string);
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
}

/**
 * Convert a base64 string to Blob
 */
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
