'use client';

import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  Transition,
  TransitionChild,
} from '@headlessui/react';

import { useTranslations } from 'next-intl';
import { TagCategoryDto, useVenueTagsStore } from '@/store/use-venue-tags-store';
import { TagChips } from './tag-chips';
import { Header } from '@/components/ui/header';

interface TagSheetProps {
  isOpen: boolean;
  onClose: () => void;
  category: TagCategoryDto;
  categoryTitle: string;
  selectedTagIds: string[];
  onToggleTag: (tagId: string) => void;
}

export function TagSheet({
  isOpen,
  onClose,
  category,
  categoryTitle,
  selectedTagIds,
  onToggleTag,
}: TagSheetProps) {
  const tTag = useTranslations('components.tagSections.tags');
  const { getGroupedTags } = useVenueTagsStore();

  const tagLabel = (value: string) => {
    const label = tTag(value);
    return label === value ? value : label;
  };

  const groups = getGroupedTags(category);
  const hasGroups = groups.length > 0;

  return (
    <Transition show={isOpen}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <TransitionChild
          enter="opacity-0"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="opacity-100"
          leaveFrom="opacity-0"
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
            <DialogPanel className="bg-form-element flex max-h-[80vh] w-full max-w-[500px] flex-col overflow-hidden rounded-t-[32px] shadow-lg transition-transform duration-300 ease-out">
                <Header
                  title={categoryTitle}
                  right={
                    <button
                      type="button"
                      onClick={onClose}
                      className="flex items-center justify-center px-2 py-1"
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
                  }
                />

                <div className="flex-1 overflow-y-auto px-4 pb-8 pt-4">
                  {hasGroups ? (
                  <div className="flex flex-col gap-6">
                    {groups.map((group, idx) => (
                      <div key={idx}>
                        {group.parent && (
                          <p className="text-secondary mb-2 text-xs font-medium uppercase tracking-wide">
                            {tagLabel(group.parent.value)}
                          </p>
                        )}
                        <TagChips
                          items={
                            group.parent
                              ? [
                                  {
                                    id: group.parent.id,
                                    label: tagLabel(group.parent.value),
                                  },
                                  ...group.children.map((c) => ({
                                    id: c.id,
                                    label: tagLabel(c.value),
                                  })),
                                ]
                              : group.children.map((c) => ({
                                  id: c.id,
                                  label: tagLabel(c.value),
                                }))
                          }
                          selectedIds={selectedTagIds}
                          onToggle={onToggleTag}
                        />
                      </div>
                    ))}
                  </div>
                ) : (
                  <TagChips
                    items={category.tags.map((t) => ({
                      id: t.id,
                      label: tagLabel(t.value),
                    }))}
                    selectedIds={selectedTagIds}
                    onToggle={onToggleTag}
                  />
                )}
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
