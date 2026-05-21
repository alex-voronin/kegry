'use client';

import { useEffect, useState, useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { useVenueTagsStore } from '@/store/use-venue-tags-store';
import { cn } from '@/lib/utils';
import styles from './venue-tag-accordion.module.css';

interface VenueTagAccordionProps {
  tagIds: string[];
  tagsEndpoint?: string;
}

const CATEGORY_ORDER: Record<string, number> = {
  PriceLevel: 0,
  HospitalityAndRules: 1,
  SmokingPolicy: 2,
  FoodAndKitchen: 3,
  InfrastructureAndAmenities: 4,
  DrinkCategory: 5,
};

const MAIN_CATEGORIES = new Set(['PriceLevel', 'HospitalityAndRules', 'DrinkCategory']);

export function VenueTagAccordion({ tagIds, tagsEndpoint }: VenueTagAccordionProps) {
  const t = useTranslations('components.tagSections');
  const tTag = useTranslations('components.tagSections.tags');
  const { fetchTags, hasHydrated, getTagLabel, getTagCategory, getTagParentId } =
    useVenueTagsStore();
  const [isExpanded, setIsExpanded] = useState(false);

  useEffect(() => {
    if (hasHydrated) {
      fetchTags(tagsEndpoint);
    }
  }, [hasHydrated, fetchTags, tagsEndpoint]);

  const resolved = useMemo(() => {
    const items: { id: string; category: string; label: string; isRoot: boolean }[] = [];
    for (const id of tagIds) {
      const category = getTagCategory(id);
      if (!category) continue;
      const raw = getTagLabel(id);
      const translated = tTag(raw);
      const parentId = getTagParentId(id);
      items.push({
        id,
        category,
        label: translated !== raw ? translated : raw,
        isRoot: parentId === null,
      });
    }
    items.sort(
      (a, b) =>
        (CATEGORY_ORDER[a.category] ?? 99) -
        (CATEGORY_ORDER[b.category] ?? 99),
    );
    return items;
  }, [tagIds, getTagCategory, getTagLabel, getTagParentId, tTag]);

  const collapsedTags = useMemo(
    () => resolved.filter((t) => t.isRoot && MAIN_CATEGORIES.has(t.category)),
    [resolved],
  );
  const hasRemaining = resolved.length > collapsedTags.length;

  const tagsByCategory = useMemo(() => {
    const map = new Map<string, { id: string; label: string }[]>();
    for (const { id, category, label } of resolved) {
      if (!map.has(category)) map.set(category, []);
      map.get(category)!.push({ id, label });
    }
    return Array.from(map.entries());
  }, [resolved]);

  if (!tagIds || tagIds.length === 0) return null;

  return (
    <div>
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        className={styles.accordionButton}
      >
        <div className={styles.chipsRow}>
          {collapsedTags.map(({ id, category, label }) => (
            <span
              key={id}
              className={`chipCat${category} inline-flex items-center rounded-full border px-2 py-0.5 text-xs leading-tight`}
              style={{
                color: 'var(--chip-text)',
                borderColor: 'var(--chip-border)',
                background: 'var(--chip-bg)',
              }}
            >
              {label}
            </span>
          ))}
          {hasRemaining && (
            <span className={styles.moreBadge}>
              +{resolved.length - collapsedTags.length}
            </span>
          )}
        </div>
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className={cn(
            styles.accordionIcon,
            isExpanded && styles.accordionIconOpen,
          )}
        >
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </button>

      {isExpanded && (
        <div className={styles.accordionContent}>
          {tagsByCategory.map(([category, items]) => (
            <div key={category} className={`${styles.category} chipCat${category}`}>
              <p className={styles.categoryTitle}>
                {t(`categories.${category}`)}
              </p>
              <div className={styles.chipsRow}>
                {items.map(({ id, label }) => (
                  <span
                    key={id}
                    className="tag-chip--selected inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs"
                  >
                    {label}
                  </span>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
