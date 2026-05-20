'use client';

import { useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useVenueTagsStore } from '@/store/use-venue-tags-store';
import styles from './venue-display-tags.module.css';

interface VenueDisplayTagsProps {
  tagIds: string[] | undefined;
}

const CATEGORY_CLASS: Record<string, string | undefined> = {
  DrinkCategory: styles.chipDrinkCategory,
  PriceLevel: styles.chipPriceLevel,
  HospitalityAndRules: styles.chipHospitalityAndRules,
};

export function VenueDisplayTags({ tagIds }: VenueDisplayTagsProps) {
  const tTag = useTranslations('components.tagSections.tags');
  const { hasHydrated, fetchTags, getTagLabel, getTagCategory } =
    useVenueTagsStore();

  useEffect(() => {
    if (hasHydrated) fetchTags();
  }, [hasHydrated, fetchTags]);

  if (!tagIds?.length) return null;

  const resolved: { id: string; category: string; label: string }[] = [];

  for (const id of tagIds) {
    const category = getTagCategory(id);
    if (!category) continue;

    const raw = getTagLabel(id);
    const translated = tTag(raw);
    const label = translated !== raw ? translated : raw;

    resolved.push({ id, category, label });
  }

  if (!resolved.length) return null;

  return (
    <div className={styles.row}>
      {resolved.map(({ id, category, label }) => (
        <span
          key={id}
          className={`${styles.chip} ${CATEGORY_CLASS[category] ?? ''}`}
        >
          {label}
        </span>
      ))}
    </div>
  );
}
