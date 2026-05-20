'use client';

import { useEffect, useRef, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Search, ListFilter, X } from 'lucide-react';
import { useVenueFiltersStore } from '@/store/use-venue-filters-store';
import { useVenueTagsStore } from '@/store/use-venue-tags-store';
import { VenueTagSheet } from './venue-tag-sheet';
import styles from './venue-filters.module.css';

export function VenueFilters() {
  const t = useTranslations('screens.venues');
  const tTag = useTranslations('components.tagSections.tags');
  const { searchQuery, selectedTagIds, setSearchQuery, setTagIds } =
    useVenueFiltersStore();
  const { getTagLabel, getTagCategory } = useVenueTagsStore();

  const [inputValue, setInputValue] = useState('');
  const [isSheetOpen, setIsSheetOpen] = useState(false);
  const [sheetKey, setSheetKey] = useState(0);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | undefined>(
    undefined,
  );

  // Sync input from store — handles rehydration and external clears
  useEffect(() => {
    setInputValue(searchQuery);
  }, [searchQuery]);

  useEffect(() => {
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, []);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const v = e.target.value;
    setInputValue(v);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => setSearchQuery(v), 600);
  };

  const handleClearInput = () => {
    setInputValue('');
    setSearchQuery('');
    if (debounceRef.current) clearTimeout(debounceRef.current);
  };

  const handleClearTags = () => {
    setTagIds([]);
  };

  const tagLabelById = (tagId: string) => {
    const raw = getTagLabel(tagId);
    return tTag(raw);
  };

  const hasTagFilters = selectedTagIds.length > 0;

  return (
    <>
      <div className={styles.wrapper}>
        <div className={styles.searchRow}>
          <div className={styles.searchInputContainer}>
            <Search size={16} className="shrink-0 text-zinc-500" />
            <input
              type="text"
              value={inputValue}
              onChange={handleInputChange}
              placeholder={t('searchVenues')}
              className={styles.searchInput}
            />
            {inputValue && (
              <button
                type="button"
                onClick={handleClearInput}
                className={styles.clearBtn}
              >
                <X size={14} />
              </button>
            )}
          </div>

          <button
            type="button"
            onClick={() => {
              setSheetKey((k) => k + 1);
              setIsSheetOpen(true);
            }}
            className={styles.tagBtn}
          >
            <ListFilter size={18} className="text-foreground" />
            {selectedTagIds.length > 0 && (
              <span className={styles.tagBtnBadge}>
                {selectedTagIds.length}
              </span>
            )}
          </button>
        </div>

        {hasTagFilters && (
          <div className={styles.chipsRow}>
            {selectedTagIds.map((id) => {
              const cat = getTagCategory(id);
              return (
                <span
                  key={id}
                  className={`${styles.chip} ${styles.chipSelected}${cat ? ` ${styles[`chipCat${cat}`]}` : ''}`}
                >
                  <span className={styles.chipLabel}>{tagLabelById(id)}</span>
                  <button
                    type="button"
                    onClick={() =>
                      setTagIds(selectedTagIds.filter((tid) => tid !== id))
                    }
                    className={styles.chipRemove}
                  >
                    <X size={12} />
                  </button>
                </span>
              );
            })}
            <button
              type="button"
              onClick={handleClearTags}
              className={styles.clearAllBtn}
            >
              {t('clear')}
            </button>
          </div>
        )}
      </div>

      <VenueTagSheet
        key={sheetKey}
        isOpen={isSheetOpen}
        onClose={() => setIsSheetOpen(false)}
      />
    </>
  );
}
