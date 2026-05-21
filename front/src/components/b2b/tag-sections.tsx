'use client';

import { useEffect, useState, useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { API_ENDPOINTS } from '@/lib/routes';
import {
  useVenueTagsStore,
  TagCategoryDto,
} from '@/store/use-venue-tags-store';
import { TagSheet } from './tag-sheet';
import { cn } from '@/lib/utils';

interface TagSectionsProps {
  selectedTagIds: string[];
  onTagIdsChange: (ids: string[]) => void;
}

const CATEGORY_META: Record<string, { order: number; essential: boolean }> = {
  PriceLevel: { order: 1, essential: true },
  FoodAndKitchen: { order: 2, essential: true },
  SmokingPolicy: { order: 3, essential: true },
  DrinkCategory: { order: 4, essential: false },
  HospitalityAndRules: { order: 5, essential: false },
  InfrastructureAndAmenities: { order: 6, essential: false },
};

export function TagSections({
  selectedTagIds,
  onTagIdsChange,
}: TagSectionsProps) {
  const t = useTranslations('components.tagSections');
  const tTag = useTranslations('components.tagSections.tags');
  const {
    categories,
    fetchTags,
    hasHydrated,
    isLoading,
    getTagLabel,
    getGroupedTags,
  } = useVenueTagsStore();

  const [sheetCategory, setSheetCategory] = useState<TagCategoryDto | null>(
    null,
  );
  const [showOptional, setShowOptional] = useState(false);

  useEffect(() => {
    if (hasHydrated) {
      fetchTags(API_ENDPOINTS.B2B.TAGS.LIST);
    }
  }, [hasHydrated, fetchTags]);

  const handleToggleTag = (tagId: string) => {
    const cat = categories.find((c) => c.tags.some((t) => t.id === tagId));
    if (!cat) return;

    const groups = getGroupedTags(cat);
    const childToParent = new Map<string, string>();
    const parentToChildren = new Map<string, string[]>();
    for (const g of groups) {
      if (!g.parent) continue;
      parentToChildren.set(
        g.parent.id,
        g.children.map((c) => c.id),
      );
      for (const c of g.children) {
        childToParent.set(c.id, g.parent.id);
      }
    }

    const parentId = childToParent.get(tagId);
    const isSelected = selectedTagIds.includes(tagId);

    let newIds: string[];
    if (cat.isSingleSelect) {
      const catIds = new Set(cat.tags.map((t) => t.id));
      newIds = selectedTagIds.filter((id) => !catIds.has(id));
      if (!isSelected) newIds.push(tagId);
    } else {
      newIds = isSelected
        ? selectedTagIds.filter((id) => id !== tagId)
        : [...selectedTagIds, tagId];

      if (parentId && !isSelected && !selectedTagIds.includes(parentId)) {
        newIds.push(parentId);
      }

      if (isSelected && parentToChildren.has(tagId)) {
        const toRemove = parentToChildren.get(tagId) || [];
        newIds = newIds.filter((id) => !toRemove.includes(id));
      }
    }

    onTagIdsChange(newIds);
  };

  const sorted = useMemo(
    () =>
      [...categories].sort((a, b) => {
        const ma = CATEGORY_META[a.category];
        const mb = CATEGORY_META[b.category];
        return (ma?.order ?? 99) - (mb?.order ?? 99);
      }),
    [categories],
  );

  const selectedInCategory = (cat: TagCategoryDto) =>
    selectedTagIds.filter((id) => cat.tags.some((t) => t.id === id));

  if (categories.length === 0) {
    if (isLoading) {
      return (
        <div className="flex flex-col gap-3">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className="bg-form-element h-12 animate-pulse rounded-xl"
            />
          ))}
        </div>
      );
    }
    return null;
  }

  const essentialCategories = sorted.filter(
    (c) => CATEGORY_META[c.category]?.essential,
  );
  const optionalCategories = sorted.filter(
    (c) => !CATEGORY_META[c.category]?.essential,
  );

  const hasOptional = optionalCategories.length > 0;
  const optionalSelectedCount = optionalCategories.reduce(
    (sum, cat) => sum + selectedInCategory(cat).length,
    0,
  );

  return (
    <div className="flex flex-col">
      {essentialCategories.map((cat) => {
        const selected = selectedInCategory(cat);
        return (
          <div className="mb-3" key={cat.category}>
            <button
              type="button"
              onClick={() => setSheetCategory(cat)}
              className={`hover:bg-hover bg-form-element flex w-full items-center justify-between rounded-xl px-3 py-3 transition-colors chipCat${cat.category}`}
            >
              <div className="flex flex-col items-start gap-0.5">
                <span className="text-sm font-medium">
                  {t(`categories.${cat.category}`)}
                </span>
                {selected.length > 0 && (
                  <div className="flex flex-wrap gap-1.5">
                    {selected.map((id) => (
                      <span
                        key={id}
                        className="tag-chip--selected inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs"
                      >
                        {tTag(getTagLabel(id))}
                      </span>
                    ))}
                  </div>
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
                className="text-secondary shrink-0"
              >
                <polyline points="9 18 15 12 9 6" />
              </svg>
            </button>
          </div>
        );
      })}

      {hasOptional && (
        <div>
          <button
            type="button"
            onClick={() => setShowOptional(!showOptional)}
            className="hover:bg-hover bg-form-element flex w-full items-center justify-between rounded-xl px-3 py-3 transition-colors"
          >
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium">{t('optional')}</span>
              {optionalSelectedCount > 0 && (
                <span className="bg-active text-primary inline-flex h-5 min-w-5 items-center justify-center rounded-full border border-white/20 px-1.5 text-xs shadow-sm">
                  {optionalSelectedCount}
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
                'text-secondary transition-transform duration-200',
                showOptional && 'rotate-180',
              )}
            >
              <polyline points="6 9 12 15 18 9" />
            </svg>
          </button>

          {showOptional && (
            <div className="mt-2 ml-2 border-l border-white/10 pl-2">
              {optionalCategories.map((cat) => (
                <div className="mb-3" key={cat.category}>
                  <button
                    type="button"
                    onClick={() => setSheetCategory(cat)}
                    className={`hover:bg-hover bg-form-element flex w-full items-center justify-between rounded-xl px-3 py-3 transition-colors chipCat${cat.category}`}
                  >
                    <div className="flex flex-col items-start gap-0.5">
                      <span className="text-sm font-medium">
                        {t(`categories.${cat.category}`)}
                      </span>
                      {selectedInCategory(cat).length > 0 && (
                        <div className="flex flex-wrap gap-1.5">
                          {selectedInCategory(cat).map((id) => (
                            <span
                              key={id}
                              className="tag-chip--selected inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs"
                            >
                              {tTag(getTagLabel(id))}
                            </span>
                          ))}
                        </div>
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
                      className="text-secondary shrink-0"
                    >
                      <polyline points="9 18 15 12 9 6" />
                    </svg>
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {sheetCategory && (
        <TagSheet
          isOpen={!!sheetCategory}
          onClose={() => setSheetCategory(null)}
          category={sheetCategory}
          categoryTitle={t(`categories.${sheetCategory.category}`)}
          selectedTagIds={selectedTagIds}
          onToggleTag={handleToggleTag}
        />
      )}
    </div>
  );
}
