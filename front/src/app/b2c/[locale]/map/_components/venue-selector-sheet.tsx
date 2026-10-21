'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  DialogTitle,
  Transition,
  TransitionChild,
} from '@headlessui/react';
import { Search, X } from 'lucide-react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { paramsForReq } from '@/lib/query-params';
import type { PaginatedResponse } from '@/lib/api-types';
import { useCitySelection } from '@/hooks/use-city-selection';
import { BELGRADE_COORDS } from '@/lib/cities';
import type { VenueMapItem } from './venue-map-internal';
import type { VenueListItemDto } from '../../venues/use-venues-store';
import styles from '../map.module.css';

interface VenueSelectorSheetProps {
  isOpen: boolean;
  onClose: () => void;
  onSelectVenue: (venueId: string, venueName: string) => void;
  initialVenues: VenueMapItem[];
}

interface VenueSearchItem {
  id: string;
  companyName: string;
  companyAvatarImageUrl: string | null;
  address: string;
}

export function VenueSelectorSheet({
  isOpen,
  onClose,
  onSelectVenue,
  initialVenues,
}: VenueSelectorSheetProps) {
  const t = useTranslations('screens.map');
  const { selectedCity } = useCitySelection();

  const [query, setQuery] = useState('');
  const [venues, setVenues] = useState<VenueSearchItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [offset, setOffset] = useState(0);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const listRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isOpen) {
      setQuery('');
      setOffset(0);
      setHasMore(true);
      setVenues(
        initialVenues.map((v) => ({
          id: v.id,
          companyName: v.companyName,
          companyAvatarImageUrl: v.companyAvatarImageUrl,
          address: v.address,
        })),
      );
    }
  }, [isOpen, initialVenues]);

  const fetchVenues = useCallback(
    async (searchQuery: string, offsetVal: number, append: boolean) => {
      setIsLoading(true);
      try {
        const lat = selectedCity?.latitude ?? BELGRADE_COORDS.latitude;
        const lon = selectedCity?.longitude ?? BELGRADE_COORDS.longitude;

        const response = await apiClient.get<
          PaginatedResponse<VenueListItemDto>
        >(
          API_ENDPOINTS.VENUES.LIST,
          paramsForReq({
            latitude: lat,
            longitude: lon,
            offset: offsetVal,
            limit: 20,
            'Filter.CompanyName': searchQuery || undefined,
          }),
        );

        const { data, totalCount: total } = response.data;
        const items: VenueSearchItem[] = data.map((d) => ({
          id: d.id,
          companyName: d.companyName,
          companyAvatarImageUrl: d.companyAvatarImageUrl,
          address: d.address,
        }));

        if (append) {
          setVenues((prev) => [...prev, ...items]);
        } else {
          setVenues(items);
        }
        setHasMore(offsetVal + items.length < total);
      } catch {
        if (!append) setVenues([]);
      } finally {
        setIsLoading(false);
      }
    },
    [selectedCity],
  );

  useEffect(() => {
    if (isOpen && initialVenues.length === 0) {
      fetchVenues('', 0, false);
    }
  }, [isOpen, initialVenues, fetchVenues]);

  const handleSearchChange = useCallback(
    (value: string) => {
      setQuery(value);
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        setOffset(0);
        fetchVenues(value, 0, false);
      }, 400);
    },
    [fetchVenues],
  );

  const handleClear = useCallback(() => {
    setQuery('');
    if (debounceRef.current) clearTimeout(debounceRef.current);
    setOffset(0);
    setHasMore(true);
    setVenues(
      initialVenues.map((v) => ({
        id: v.id,
        companyName: v.companyName,
        companyAvatarImageUrl: v.companyAvatarImageUrl,
        address: v.address,
      })),
    );
  }, [initialVenues]);

  const handleScroll = useCallback(() => {
    const el = listRef.current;
    if (!el || isLoading || !hasMore) return;
    if (el.scrollHeight - el.scrollTop - el.clientHeight < 200) {
      const newOffset = offset + 20;
      setOffset(newOffset);
      fetchVenues(query, newOffset, true);
    }
  }, [isLoading, hasMore, offset, fetchVenues, query]);

  const handleSelect = useCallback(
    (venueId: string, venueName: string) => {
      onSelectVenue(venueId, venueName);
      onClose();
    },
    [onSelectVenue, onClose],
  );

  return (
    <Transition show={isOpen}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <TransitionChild
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <DialogBackdrop className={styles.selectorBackdrop} />
        </TransitionChild>

        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <TransitionChild
            enter="ease-out duration-300"
            enterFrom="opacity-0 scale-95"
            enterTo="opacity-100 scale-100"
            leave="ease-in duration-200"
            leaveFrom="opacity-100 scale-100"
            leaveTo="opacity-0 scale-95"
          >
            <DialogPanel className={styles.selectorPanel}>
              <DialogTitle className={styles.selectorTitle}>
                {t('venueSelector.title')}
              </DialogTitle>

              <div className={styles.selectorSearchContainer}>
                <Search size={16} className="shrink-0 text-zinc-500" />
                <input
                  type="text"
                  value={query}
                  onChange={(e) => handleSearchChange(e.target.value)}
                  placeholder={t('venueSelector.searchPlaceholder')}
                  className={styles.selectorSearchInput}
                  autoFocus
                />
                {query && (
                  <button
                    type="button"
                    onClick={handleClear}
                    className="shrink-0 text-zinc-500"
                  >
                    <X size={14} />
                  </button>
                )}
              </div>

              <div
                ref={listRef}
                onScroll={handleScroll}
                className={styles.selectorList}
              >
                {venues.length === 0 && !isLoading && (
                  <p className={styles.selectorEmpty}>
                    {query
                      ? t('nothingFound')
                      : t('venueSelector.typeToSearch')}
                  </p>
                )}

                {venues.map((venue) => (
                  <button
                    key={venue.id}
                    type="button"
                    className={styles.selectorItem}
                    onClick={() => handleSelect(venue.id, venue.companyName)}
                  >
                    <Image
                      src={
                        venue.companyAvatarImageUrl ||
                        '/assets/common/venues-icon.svg'
                      }
                      alt={venue.companyName}
                      width={40}
                      height={40}
                      className="h-10 w-10 shrink-0 rounded-full object-cover"
                    />
                    <div className="min-w-0 flex-1 text-left">
                      <div className="truncate text-sm font-medium text-white">
                        {venue.companyName}
                      </div>
                      <div className="truncate text-xs text-white/50">
                        {venue.address}
                      </div>
                    </div>
                  </button>
                ))}

                {isLoading && (
                  <div className="flex justify-center py-4">
                    <div className="h-5 w-5 animate-spin rounded-full border-2 border-white/30 border-t-white" />
                  </div>
                )}

                {!hasMore && venues.length > 0 && <div className="h-4" />}
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
