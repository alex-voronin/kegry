'use client';

import { useMemo, useEffect } from 'react';
import { useVenueTagsStore } from '@/store/use-venue-tags-store';
import { useTranslations } from 'next-intl';

const CATEGORY_ORDER: Record<string, number> = {
  PriceLevel: 0,
  HospitalityAndRules: 1,
  SmokingPolicy: 2,
  FoodAndKitchen: 3,
  InfrastructureAndAmenities: 4,
  DrinkCategory: 5,
};

export interface TagChipItem {
  id: string;
  category: string;
  label: string;
}

export function useVenueTagChips(tagIds: string[], tagsEndpoint?: string, max = 10) {
  const tTag = useTranslations('components.tagSections.tags');
  const { fetchTags, hasHydrated, getTagLabel, getTagCategory, categories } =
    useVenueTagsStore();

  useEffect(() => {
    if (hasHydrated) {
      fetchTags(tagsEndpoint);
    }
  }, [hasHydrated, fetchTags, tagsEndpoint]);

  return useMemo(() => {
    const items: TagChipItem[] = [];
    for (const id of tagIds) {
      const category = getTagCategory(id);
      if (!category) continue;
      const raw = getTagLabel(id);
      const translated = tTag(raw);
      items.push({
        id,
        category,
        label: translated !== raw ? translated : raw,
      });
    }

    const sortIndex = new Map<string, number>();
    for (const cat of categories) {
      for (let i = 0; i < cat.tags.length; i++) {
        sortIndex.set(cat.tags[i].id, i);
      }
    }

    items.sort((a, b) => {
      const catDiff =
        (CATEGORY_ORDER[a.category] ?? 99) - (CATEGORY_ORDER[b.category] ?? 99);
      if (catDiff !== 0) return catDiff;
      return (sortIndex.get(a.id) ?? 99) - (sortIndex.get(b.id) ?? 99);
    });

    const remaining = items.length > max ? items.length - max : 0;
    const visible = remaining > 0 ? items.slice(0, max) : items;

    return { resolved: items, visible, remaining, isEmpty: items.length === 0 };
  }, [tagIds, getTagCategory, getTagLabel, categories, tTag, max]);
}
