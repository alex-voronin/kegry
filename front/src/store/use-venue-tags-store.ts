'use client';

import { create } from '@/lib/zustand-reset';
import { persist } from 'zustand/middleware';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';

export interface TagDto {
  id: string;
  value: string;
  parentId: string | null;
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

  fetchTags: (endpoint?: string) => Promise<void>;
  getGroupedTags: (category: TagCategoryDto) => TagGroup[];
  getTagLabel: (tagId: string) => string;
  getTagCategory: (tagId: string) => string | null;
  getTagParentId: (tagId: string) => string | null;
  setHasHydrated: (hydrated: boolean) => void;
  reset: () => void;
}

const CACHE_DURATION_MS = 5 * 60 * 1000;

interface ApiTagDto {
  id: string;
  value: string;
  parentId: string | null;
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

function mockId(seed: string): string {
  let hash = 0;
  for (let i = 0; i < seed.length; i++) {
    const char = seed.charCodeAt(i);
    hash = (hash << 5) - hash + char;
    hash |= 0;
  }
  const hex = (hash >>> 0).toString(16).padStart(8, '0');
  return `${hex}-0000-0000-0000-000000000000`.slice(0, 36);
}

function mockTagCategories(): TagCategoryDto[] {
  return [
    {
      category: 'DrinkCategory',
      isSingleSelect: false,
      tags: [
        { id: mockId('beer'), value: 'beer', parentId: null },
        { id: mockId('cider'), value: 'cider', parentId: null },
        { id: mockId('wine'), value: 'wine', parentId: null },
        { id: mockId('spirit'), value: 'spirit', parentId: null },
        { id: mockId('cocktail'), value: 'cocktail', parentId: null },
        { id: mockId('non_alcoholic'), value: 'non_alcoholic', parentId: null },
        {
          id: mockId('beer_craft'),
          value: 'beer_craft',
          parentId: mockId('beer'),
        },
        {
          id: mockId('beer_lager'),
          value: 'beer_lager',
          parentId: mockId('beer'),
        },
        { id: mockId('beer_ipa'), value: 'beer_ipa', parentId: mockId('beer') },
        {
          id: mockId('beer_stout'),
          value: 'beer_stout',
          parentId: mockId('beer'),
        },
        {
          id: mockId('beer_sour'),
          value: 'beer_sour',
          parentId: mockId('beer'),
        },
        {
          id: mockId('beer_wheat'),
          value: 'beer_wheat',
          parentId: mockId('beer'),
        },
        {
          id: mockId('beer_fruit'),
          value: 'beer_fruit',
          parentId: mockId('beer'),
        },
        {
          id: mockId('cider_apple'),
          value: 'cider_apple',
          parentId: mockId('cider'),
        },
        {
          id: mockId('cider_pear'),
          value: 'cider_pear',
          parentId: mockId('cider'),
        },
        {
          id: mockId('cider_dry'),
          value: 'cider_dry',
          parentId: mockId('cider'),
        },
        {
          id: mockId('cider_sweet'),
          value: 'cider_sweet',
          parentId: mockId('cider'),
        },
        { id: mockId('wine_red'), value: 'wine_red', parentId: mockId('wine') },
        {
          id: mockId('wine_white'),
          value: 'wine_white',
          parentId: mockId('wine'),
        },
        {
          id: mockId('wine_orange'),
          value: 'wine_orange',
          parentId: mockId('wine'),
        },
        {
          id: mockId('wine_sparkling'),
          value: 'wine_sparkling',
          parentId: mockId('wine'),
        },
        {
          id: mockId('spirit_vodka'),
          value: 'spirit_vodka',
          parentId: mockId('spirit'),
        },
        {
          id: mockId('spirit_whiskey'),
          value: 'spirit_whiskey',
          parentId: mockId('spirit'),
        },
        {
          id: mockId('spirit_gin'),
          value: 'spirit_gin',
          parentId: mockId('spirit'),
        },
        {
          id: mockId('spirit_rum'),
          value: 'spirit_rum',
          parentId: mockId('spirit'),
        },
        {
          id: mockId('spirit_tequila'),
          value: 'spirit_tequila',
          parentId: mockId('spirit'),
        },
        {
          id: mockId('spirit_mezcal'),
          value: 'spirit_mezcal',
          parentId: mockId('spirit'),
        },
        {
          id: mockId('spirit_brandy'),
          value: 'spirit_brandy',
          parentId: mockId('spirit'),
        },
        {
          id: mockId('cocktail_classic'),
          value: 'cocktail_classic',
          parentId: mockId('cocktail'),
        },
        {
          id: mockId('cocktail_signature'),
          value: 'cocktail_signature',
          parentId: mockId('cocktail'),
        },
        {
          id: mockId('cocktail_spritz'),
          value: 'cocktail_spritz',
          parentId: mockId('cocktail'),
        },
        {
          id: mockId('coffee'),
          value: 'coffee',
          parentId: mockId('non_alcoholic'),
        },
        { id: mockId('tea'), value: 'tea', parentId: mockId('non_alcoholic') },
        {
          id: mockId('beverages'),
          value: 'beverages',
          parentId: mockId('non_alcoholic'),
        },
      ],
    },
    {
      category: 'HospitalityAndRules',
      isSingleSelect: false,
      tags: [
        { id: mockId('pet_friendly'), value: 'pet_friendly', parentId: null },
        { id: mockId('kids_friendly'), value: 'kids_friendly', parentId: null },
        {
          id: mockId('coworking_friendly'),
          value: 'coworking_friendly',
          parentId: null,
        },
      ],
    },
    {
      category: 'FoodAndKitchen',
      isSingleSelect: false,
      tags: [
        { id: mockId('kitchen_full'), value: 'kitchen_full', parentId: null },
        {
          id: mockId('kitchen_snacks_only'),
          value: 'kitchen_snacks_only',
          parentId: null,
        },
        {
          id: mockId('kitchen_external_allowed'),
          value: 'kitchen_external_allowed',
          parentId: null,
        },
        { id: mockId('vegan_options'), value: 'vegan_options', parentId: null },
      ],
    },
    {
      category: 'InfrastructureAndAmenities',
      isSingleSelect: false,
      tags: [
        {
          id: mockId('terrace_summer'),
          value: 'terrace_summer',
          parentId: null,
        },
        {
          id: mockId('terrace_all_year'),
          value: 'terrace_all_year',
          parentId: null,
        },
        {
          id: mockId('parking_available'),
          value: 'parking_available',
          parentId: null,
        },
        { id: mockId('bike_rack'), value: 'bike_rack', parentId: null },
        { id: mockId('rooftop'), value: 'rooftop', parentId: null },
      ],
    },
    {
      category: 'SmokingPolicy',
      isSingleSelect: true,
      tags: [
        {
          id: mockId('smoking_allowed_inside'),
          value: 'smoking_allowed_inside',
          parentId: null,
        },
        {
          id: mockId('smoking_allowed_outside_only'),
          value: 'smoking_allowed_outside_only',
          parentId: null,
        },
        {
          id: mockId('smoking_totally_forbidden'),
          value: 'smoking_totally_forbidden',
          parentId: null,
        },
        {
          id: mockId('smoking_dedicated_room_inside'),
          value: 'smoking_dedicated_room_inside',
          parentId: null,
        },
      ],
    },
    {
      category: 'PriceLevel',
      isSingleSelect: true,
      tags: [
        { id: mockId('price_budget'), value: 'price_budget', parentId: null },
        { id: mockId('price_average'), value: 'price_average', parentId: null },
        { id: mockId('price_premium'), value: 'price_premium', parentId: null },
      ],
    },
  ];
}

export const useVenueTagsStore = create<VenueTagsState>()(
  persist(
    (set, get) => ({
      ...initialState,

      fetchTags: async (endpoint?: string) => {
        const state = get();
        const now = Date.now();

        if (
          state.lastFetched &&
          now - state.lastFetched < CACHE_DURATION_MS &&
          state.categories.length > 0
        ) {
          return;
        }

        set({ isLoading: true });

        try {
          const response = await apiClient.get<ApiTagCategoryDto[]>(
            endpoint ?? API_ENDPOINTS.TAGS.LIST,
          );

          const categories = response.data.map((cat) => ({
            category: cat.category,
            isSingleSelect: cat.isSingleSelect,
            tags: cat.tags.map((t) => ({
              id: t.id,
              value: t.value,
              parentId: t.parentId,
            })),
          }));

          set({
            categories,
            lastFetched: Date.now(),
            isLoading: false,
          });
        } catch (error) {
          set({
            categories: mockTagCategories(),
            lastFetched: Date.now(),
            isLoading: false,
          });
        }
      },

      getGroupedTags: (category: TagCategoryDto): TagGroup[] => {
        const rootTags = category.tags.filter((t) => !t.parentId);
        const childTags = category.tags.filter((t) => t.parentId);

        if (rootTags.length === 0 && childTags.length === 0) return [];

        if (childTags.length === 0) {
          return [{ parent: null, children: rootTags }];
        }

        const parentMap = new Map<string, TagDto>();
        for (const tag of rootTags) {
          parentMap.set(tag.id, tag);
        }

        const groups: TagGroup[] = [];

        for (const parent of rootTags) {
          const kids = childTags.filter((c) => c.parentId === parent.id);
          groups.push({ parent, children: kids });
        }

        const orphanChildren = childTags.filter(
          (c) => !parentMap.has(c.parentId!),
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

      getTagParentId: (tagId: string): string | null => {
        const state = get();
        for (const cat of state.categories) {
          const tag = cat.tags.find((t) => t.id === tagId);
          if (tag) return tag.parentId;
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
