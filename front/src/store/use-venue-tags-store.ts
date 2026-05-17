'use client';

import { create } from '@/lib/zustand-reset';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface TagDto {
  id: string;
  value: string;
  parentValue: string | null;
}

export interface TagCategoryDto {
  category: string;
  isSingleSelect: boolean;
  tags: TagDto[];
}

export interface TagGroup {
  parent: TagDto | null;
  children: TagDto[];
}

interface VenueTagsState {
  categories: TagCategoryDto[];
  lastFetched: number | null;
  isLoading: boolean;
  hasHydrated: boolean;

  fetchTags: () => Promise<void>;
  getGroupedTags: (category: TagCategoryDto) => TagGroup[];
  getTagLabel: (tagId: string) => string;
  getTagCategory: (tagId: string) => string | null;
  setHasHydrated: (hydrated: boolean) => void;
  reset: () => void;
}

const CACHE_DURATION_MS = 5 * 60 * 1000;

interface ApiTagDto {
  id: string;
  value: string;
  parentValue: string | null;
}

interface ApiTagCategoryDto {
  category: string;
  isSingleSelect: boolean;
  tags: ApiTagDto[];
}

const initialState = {
  categories: [] as TagCategoryDto[],
  lastFetched: null as number | null,
  isLoading: false,
  hasHydrated: false,
};

let fetchInitiated = false;

export const useVenueTagsStore = create<VenueTagsState>()(
  persist(
    (set, get) => ({
      ...initialState,

      fetchTags: async () => {
        if (fetchInitiated) return;

        const state = get();
        const now = Date.now();

        if (
          state.lastFetched &&
          now - state.lastFetched < CACHE_DURATION_MS &&
          state.categories.length > 0
        ) {
          return;
        }

        fetchInitiated = true;

        set({ isLoading: true });

        try {
          const response = await apiClient.get<ApiResult<ApiTagCategoryDto[]>>(
            API_ENDPOINTS.TAGS.LIST,
          );

          const categories = response.data.result.map((cat) => ({
            category: cat.category,
            isSingleSelect: cat.isSingleSelect,
            tags: cat.tags.map((t) => ({
              id: t.id,
              value: t.value,
              parentValue: t.parentValue,
            })),
          }));

          set({
            categories,
            lastFetched: Date.now(),
            isLoading: false,
          });
        } catch (error) {
          set({ isLoading: false });
          rethrowApiError(error, 'Failed to fetch tags');
        }
      },

      getGroupedTags: (category: TagCategoryDto): TagGroup[] => {
        const rootTags = category.tags.filter((t) => !t.parentValue);
        const childTags = category.tags.filter((t) => t.parentValue);

        if (rootTags.length === 0 && childTags.length === 0) return [];

        if (childTags.length === 0) {
          return [{ parent: null, children: rootTags }];
        }

        const parentMap = new Map<string, TagDto>();
        for (const tag of rootTags) {
          parentMap.set(tag.value, tag);
        }

        const groups: TagGroup[] = [];

        for (const parent of rootTags) {
          const kids = childTags.filter((c) => c.parentValue === parent.value);
          groups.push({ parent, children: kids });
        }

        const orphanChildren = childTags.filter(
          (c) => !parentMap.has(c.parentValue!),
        );
        if (orphanChildren.length > 0) {
          groups.push({ parent: null, children: orphanChildren });
        }

        return groups;
      },

      getTagLabel: (tagId: string): string => {
        const state = get();
        for (const cat of state.categories) {
          const tag = cat.tags.find((t) => t.id === tagId);
          if (tag) return tag.value;
        }
        return tagId;
      },

      getTagCategory: (tagId: string): string | null => {
        const state = get();
        for (const cat of state.categories) {
          if (cat.tags.some((t) => t.id === tagId)) return cat.category;
        }
        return null;
      },

      setHasHydrated: (hydrated) => set({ hasHydrated: hydrated }),
      reset: () => set(initialState),
    }),
    {
      name: 'venue-tags-store',
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
      partialize: (state) => ({
        categories: state.categories,
        lastFetched: state.lastFetched,
      }),
    },
  ),
);
