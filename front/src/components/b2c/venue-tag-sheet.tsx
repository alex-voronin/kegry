'use client';

import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  Transition,
  TransitionChild,
} from '@headlessui/react';

import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useVenueTagsStore } from '@/store/use-venue-tags-store';
import { useVenueFiltersStore } from '@/store/use-venue-filters-store';
import { TagChips } from '@/components/b2b/tag-chips';
import styles from './venue-tag-sheet.module.css';

interface VenueTagSheetProps {
  isOpen: boolean;
  onClose: () => void;
}

const CATEGORY_ORDER: Record<
  string,
  { priority: number; alwaysVisible: boolean }
> = {
  DrinkCategory: { priority: 1, alwaysVisible: true },
  PriceLevel: { priority: 2, alwaysVisible: true },
  HospitalityAndRules: { priority: 3, alwaysVisible: false },
  SmokingPolicy: { priority: 4, alwaysVisible: false },
  FoodAndKitchen: { priority: 5, alwaysVisible: false },
  InfrastructureAndAmenities: { priority: 6, alwaysVisible: false },
};

export function VenueTagSheet({ isOpen, onClose }: VenueTagSheetProps) {
  const tCat = useTranslations('components.tagSections');
  const tTag = useTranslations('components.tagSections.tags');
  const tVenues = useTranslations('screens.venues');
  const { categories, isLoading, fetchTags, hasHydrated } = useVenueTagsStore();
  const { selectedTagIds, setTagIds } = useVenueFiltersStore();
  const [pendingTagIds, setPendingTagIds] = useState<string[]>(selectedTagIds);
  const [showOptional, setShowOptional] = useState(false);

  useEffect(() => {
    if (hasHydrated) {
      fetchTags();
    }
  }, [hasHydrated, fetchTags]);

  const handleToggleTag = (tagId: string) => {
    const cat = categories.find((c) => c.tags.some((t) => t.id === tagId));
    if (!cat) return;

    const isSelected = pendingTagIds.includes(tagId);

    let newIds: string[];
    if (cat.isSingleSelect) {
      const catIds = new Set(cat.tags.map((t) => t.id));
      newIds = pendingTagIds.filter((id) => !catIds.has(id));
      if (!isSelected) newIds.push(tagId);
    } else {
      newIds = isSelected
        ? pendingTagIds.filter((id) => id !== tagId)
        : [...pendingTagIds, tagId];
    }

    setPendingTagIds(newIds);
  };

  const handleClose = () => {
    setTagIds(pendingTagIds);
    onClose();
  };

  const sorted = [...categories].sort((a, b) => {
    const ma = CATEGORY_ORDER[a.category];
    const mb = CATEGORY_ORDER[b.category];
    return (ma?.priority ?? 99) - (mb?.priority ?? 99);
  });

  const drinkCategory = sorted.find((c) => c.category === 'DrinkCategory');

  const alwaysVisible = sorted.filter(
    (c) => CATEGORY_ORDER[c.category]?.alwaysVisible,
  );
  const optional = sorted.filter(
    (c) => !CATEGORY_ORDER[c.category]?.alwaysVisible,
  );

  const selectedInOptional =
    optional.reduce(
      (sum, cat) =>
        sum +
        pendingTagIds.filter((id) => cat.tags.some((t) => t.id === id)).length,
      0,
    ) +
    (drinkCategory
      ? pendingTagIds.filter((id) =>
          drinkCategory.tags.some((t) => t.id === id && t.parentId),
        ).length
      : 0);

  const tagLabel = (value: string) => {
    const label = tTag(value);
    return label === value ? value : label;
  };

  const renderFlatCategory = (cat: (typeof categories)[number]) => {
    const rootTags = cat.tags.filter((t) => !t.parentId);
    return (
      <div
        key={cat.category}
        className={`${styles.category} chipCat${cat.category}`}
      >
        <p className={styles.categoryTitle}>
          {tCat(`categories.${cat.category}`)}
        </p>
        <TagChips
          items={rootTags.map((t) => ({ id: t.id, label: tagLabel(t.value) }))}
          selectedIds={pendingTagIds}
          onToggle={handleToggleTag}
          category={cat.category}
        />
      </div>
    );
  };

  const renderCategory = (cat: (typeof categories)[number]) => {
    return (
      <div
        key={cat.category}
        className={`${styles.category} chipCat${cat.category}`}
      >
        <p className={styles.categoryTitle}>
          {tCat(`categories.${cat.category}`)}
        </p>
        <TagChips
          items={cat.tags.map((t) => ({ id: t.id, label: tagLabel(t.value) }))}
          selectedIds={pendingTagIds}
          onToggle={handleToggleTag}
          category={cat.category}
        />
      </div>
    );
  };

  const renderDrinkChildren = (cat: (typeof categories)[number]) => {
    const rootTags = cat.tags.filter((t) => !t.parentId);
    const childTags = cat.tags.filter((t) => t.parentId);

    return rootTags.map((parent) => {
      const children = childTags.filter((c) => c.parentId === parent.id);
      if (children.length === 0) return null;
      return (
        <div key={parent.id}>
          <p className={styles.categoryTitle}>{tagLabel(parent.value)}</p>
          <TagChips
            items={children.map((c) => ({
              id: c.id,
              label: tagLabel(c.value),
            }))}
            selectedIds={pendingTagIds}
            onToggle={handleToggleTag}
            category={cat.category}
          />
        </div>
      );
    });
  };

  return (
    <Transition show={isOpen}>
      <Dialog
        as="div"
        className="relative z-50"
        open={isOpen}
        onClose={handleClose}
      >
        <TransitionChild
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <DialogBackdrop className="fixed inset-0 bg-black/50 backdrop-blur-xs transition-opacity duration-300" />
        </TransitionChild>

        <div className="fixed inset-0 z-50 flex items-end justify-center">
          <TransitionChild
            enter="transition duration-300 ease-out"
            enterFrom="translate-y-full"
            enterTo="translate-y-0"
            leave="transition duration-300 ease-in"
            leaveFrom="translate-y-0"
            leaveTo="translate-y-full"
          >
            <DialogPanel className={styles.dialogPanel}>
              <div className={styles.header}>
                <div className={styles.spacer} />
                <h2 className={styles.title}>{tVenues('filterByTags')}</h2>
                <button
                  type="button"
                  onClick={handleClose}
                  className={styles.closeButton}
                  aria-label="Close"
                >
                  <svg
                    width="20"
                    height="20"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <line x1="18" y1="6" x2="6" y2="18" />
                    <line x1="6" y1="6" x2="18" y2="18" />
                  </svg>
                </button>
              </div>

              <div className={styles.content}>
                {isLoading && categories.length === 0 ? (
                  <div className={styles.skeletonContainer}>
                    {[1, 2, 3].map((i) => (
                      <div key={i} className={styles.skeleton} />
                    ))}
                  </div>
                ) : (
                  <>
                    {alwaysVisible.map((cat) =>
                      cat.category === 'DrinkCategory'
                        ? renderFlatCategory(cat)
                        : renderCategory(cat),
                    )}

                    {optional.length > 0 && (
                      <div>
                        <button
                          type="button"
                          onClick={() => setShowOptional(!showOptional)}
                          className={styles.accordionButton}
                        >
                          <div className={styles.accordionLabelContainer}>
                            <span className={styles.accordionLabel}>
                              {tCat('optional')}
                            </span>
                            {selectedInOptional > 0 && (
                              <span className={styles.accordionBadge}>
                                {selectedInOptional}
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
                            className={`${styles.accordionIcon}${showOptional ? ` ${styles.accordionIconOpen}` : ''}`}
                          >
                            <polyline points="6 9 12 15 18 9" />
                          </svg>
                        </button>

                        {showOptional && (
                          <div className={styles.accordionContent}>
                            {optional.map(renderCategory)}
                            {drinkCategory && (
                              <div
                                className={`${styles.category} chipCatDrinkCategory`}
                              >
                                <div className={styles.drinkChildrenContainer}>
                                  {renderDrinkChildren(drinkCategory)}
                                </div>
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    )}
                  </>
                )}
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
