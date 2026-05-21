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

const SHOW_MAX = 12;

export function VenueTagAccordion({ tagIds, tagsEndpoint }: VenueTagAccordionProps) {
  const t = useTranslations('components.tagSections');
  const tTag = useTranslations('components.tagSections.tags');
  const { fetchTags, hasHydrated, getTagLabel, getTagCategory, getTagParentId, categories } =
    useVenueTagsStore();
  const [isExpanded, setIsExpanded] = useState(false);

  useEffect(() => {
    if (hasHydrated) {
      fetchTags(tagsEndpoint);
    }
  }, [hasHydrated, fetchTags, tagsEndpoint]);

  const resolved = useMemo(() => {
    const items: { id: string; category: string; label: string; value: string; isRoot: boolean; parentId: string | null }[] = [];
    for (const id of tagIds) {
      const category = getTagCategory(id);
      if (!category) continue;
      const value = getTagLabel(id);
      const translated = tTag(value);
      const pid = getTagParentId(id);
      items.push({
        id,
        category,
        value,
        label: translated !== value ? translated : value,
        isRoot: pid === null,
        parentId: pid,
      });
    }

    const sortIndex = new Map<string, number>();
    for (const cat of categories) {
      for (let i = 0; i < cat.tags.length; i++) {
        sortIndex.set(cat.tags[i].id, i);
      }
    }

    const sorted: typeof items = [];
    for (const cat of Object.keys(CATEGORY_ORDER)) {
      const catItems = items.filter((t) => t.category === cat);
      if (catItems.length === 0) continue;

      catItems.sort(
        (a, b) => (sortIndex.get(a.id) ?? 99) - (sortIndex.get(b.id) ?? 99),
      );
      sorted.push(...catItems);
    }

    return sorted;
  }, [tagIds, getTagCategory, getTagLabel, getTagParentId, tTag, categories]);

  const remaining = resolved.length > SHOW_MAX ? resolved.length - SHOW_MAX : 0;
  const visibleTags = remaining > 0 ? resolved.slice(0, SHOW_MAX) : resolved;

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
        <div className={styles.accordionBody}>
          <div className={styles.chipsRow}>
            {visibleTags.map(({ id, category, label }) => (
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
            {remaining > 0 && (
              <span className={styles.moreBadge}>
                +{remaining}
              </span>
            )}
          </div>
        </div>
        <div className={styles.accordionChevron}>
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
        </div>
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
