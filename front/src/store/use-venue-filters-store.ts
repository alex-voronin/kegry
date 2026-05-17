'use client';

import { create } from '@/lib/zustand-reset';
import { persist, createJSONStorage } from 'zustand/middleware';

interface VenueFiltersState {
  searchQuery: string;
  selectedTagIds: string[];
  hasHydrated: boolean;

  setSearchQuery: (query: string) => void;
  setTagIds: (ids: string[]) => void;
  clearSearch: () => void;
  clearTags: () => void;
  clearAll: () => void;
  setHasHydrated: (hydrated: boolean) => void;
}

export const useVenueFiltersStore = create<VenueFiltersState>()(
  persist(
    (set) => ({
      searchQuery: '',
      selectedTagIds: [] as string[],
      hasHydrated: false,

      setSearchQuery: (query) => set({ searchQuery: query }),
      setTagIds: (ids) => set({ selectedTagIds: ids }),
      clearSearch: () => set({ searchQuery: '' }),
      clearTags: () => set({ selectedTagIds: [] }),
      clearAll: () => set({ searchQuery: '', selectedTagIds: [] }),
      setHasHydrated: (hydrated) => set({ hasHydrated: hydrated }),
    }),
    {
      name: 'venue-filters-store',
      storage: createJSONStorage(() => sessionStorage),
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
      partialize: (state) => ({
        searchQuery: state.searchQuery,
        selectedTagIds: state.selectedTagIds,
      }),
    },
  ),
);
