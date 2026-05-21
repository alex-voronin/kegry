'use client';

import { useState, useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { cn } from '@/lib/utils';
import { useVenueTagChips } from './use-venue-tag-chips';
import styles from './venue-tag-accordion.module.css';

interface VenueTagAccordionProps {
  tagIds: string[];
  tagsEndpoint?: string;
}

export function VenueTagAccordion({ tagIds, tagsEndpoint }: VenueTagAccordionProps) {
  const t = useTranslations('components.tagSections');
  const [isExpanded, setIsExpanded] = useState(false);

  const { resolved, visible, remaining, isEmpty } = useVenueTagChips(tagIds, tagsEndpoint);

  const tagsByCategory = useMemo(() => {
    const map = new Map<string, { id: string; label: string }[]>();
    for (const { id, category, label } of resolved) {
      if (!map.has(category)) map.set(category, []);
      map.get(category)!.push({ id, label });
    }
    return Array.from(map.entries());
  }, [resolved]);

  if (isEmpty) return null;

  return (
    <div>
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        className={styles.accordionButton}
      >
        <div className={styles.accordionBody}>
        <div className={styles.chipsRow}>
            {visible.map(({ id, category, label }) => (
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
