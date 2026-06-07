'use client';

import {
  useRef,
  useState,
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
} from 'react';
import { useTranslations } from 'next-intl';
import Image from 'next/image';
import {
  ChevronDown,
  MoreHorizontal,
  CheckCircle,
  Search,
  ListFilter,
  X,
  Plus,
} from 'lucide-react';
import { ListItem } from '@/components/ui/list-item';
import { ModalMenu } from '@/components/ui/modal-menu';
import { VenueDisplayTags } from '@/components/b2c/venue-display-tags';
import { VenueTagSheet } from '@/components/b2c/venue-tag-sheet';
import { useVenueFiltersStore } from '@/store/use-venue-filters-store';
import { useVenueTagsStore } from '@/store/use-venue-tags-store';
import filtersStyles from '@/components/b2c/venue-filters.module.css';
import styles from '../map.module.css';
import type { VenueMapItem } from '../../venues/map/_components/venue-map-internal';
import type { EventMapItem } from '../_components/types';

const PEEK_HEIGHT = 176;
const EXPANDED_TOP = 190;
const TAP_MOVE_THRESHOLD = 50;

interface VenueBottomSheetProps {
  venues: VenueMapItem[];
  events: EventMapItem[];
  isLoading: boolean;
  onFlyTo?: (lat: number, lng: number, venue?: VenueMapItem) => void;
  onCenter?: (lat: number, lng: number, venue?: VenueMapItem) => void;
  onJoinEvent?: (eventId: string) => void;
  onLeaveEvent?: (eventId: string) => void;
  sheetState: 'collapsed' | 'expanded';
  onSheetStateChange: (state: 'collapsed' | 'expanded') => void;
  enabledEventTypes: ('Event' | 'Meetup')[];
  onToggleEventType: (type: 'Event' | 'Meetup') => void;
  onAddMeetup?: () => void;
}

type SheetState = 'collapsed' | 'expanded';

export function VenueBottomSheet({
  venues,
  events,
  isLoading,
  onFlyTo,
  onCenter,
  onJoinEvent,
  onLeaveEvent,
  sheetState,
  onSheetStateChange,
  enabledEventTypes,
  onToggleEventType,
  onAddMeetup,
}: VenueBottomSheetProps) {
  const t = useTranslations('screens.map');

  const [menuTarget, setMenuTarget] = useState<{
    lat: number;
    lng: number;
    venue?: VenueMapItem;
    eventId?: string;
    isJoined?: boolean;
  } | null>(null);
  const [translateY, setTranslateY] = useState(() => {
    if (typeof window === 'undefined') return 0;
    return Math.max(window.innerHeight - 60 - PEEK_HEIGHT, EXPANDED_TOP);
  });
  const [isDragging, setIsDragging] = useState(false);
  const [isFilterSheetOpen, setIsFilterSheetOpen] = useState(false);

  const tVenues = useTranslations('screens.venues');
  const tTag = useTranslations('components.tagSections.tags');
  const { searchQuery, selectedTagIds, setSearchQuery, setTagIds } =
    useVenueFiltersStore();
  const { getTagLabel, getTagCategory } = useVenueTagsStore();

  const [inputValue, setInputValue] = useState('');
  const debounceRef = useRef<ReturnType<typeof setTimeout> | undefined>(
    undefined,
  );

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
  const hasEventTypeFilter = enabledEventTypes.length < 2;
  const filterCount =
    (searchQuery ? 1 : 0) +
    selectedTagIds.length +
    (hasEventTypeFilter ? 1 : 0);
  const hasActiveFilters = !!(
    searchQuery ||
    hasTagFilters ||
    hasEventTypeFilter
  );

  const [sheetKey, setSheetKey] = useState(0);

  const sheetRef = useRef<HTMLDivElement>(null);
  const dragAreaRef = useRef<HTMLDivElement>(null);
  const startYRef = useRef(0);
  const startTranslateRef = useRef(0);
  const rafRef = useRef<number | null>(null);
  const velocityRef = useRef<{ y: number; t: number }[]>([]);
  const hasMovedRef = useRef(false);
  const totalDragRef = useRef(0);
  const currentTranslateRef = useRef(0);
  const transitionEnabledRef = useRef(false);
  const touchInProgressRef = useRef(false);
  const endDragRef = useRef<() => void>(() => {});
  const handlePointerDownRef = useRef<(clientY: number) => void>(() => {});
  const clampTranslateRef = useRef<(clientY: number) => void>(() => {});
  const recordVelocityRef = useRef<(clientY: number) => void>(() => {});

  const getSnapTranslate = useCallback((s: SheetState) => {
    const el = sheetRef.current;
    if (!el) return s === 'collapsed' ? PEEK_HEIGHT : EXPANDED_TOP;
    const h = el.clientHeight;
    if (s === 'collapsed') return Math.max(h - PEEK_HEIGHT, EXPANDED_TOP);
    return EXPANDED_TOP;
  }, []);

  useLayoutEffect(() => {
    const el = sheetRef.current;
    if (!el || isDragging) return;
    setTranslateY(getSnapTranslate(sheetState));
    transitionEnabledRef.current = true;
  }, [sheetState, getSnapTranslate, isDragging]);

  const getMaxTranslate = useCallback(() => {
    const el = sheetRef.current;
    if (!el) return PEEK_HEIGHT;
    return Math.max(el.clientHeight - PEEK_HEIGHT, EXPANDED_TOP);
  }, []);

  const clampTranslate = useCallback(
    (clientY: number) => {
      const dy = clientY - startYRef.current;
      const raw = startTranslateRef.current + dy;
      const maxTranslate = getMaxTranslate();
      const clamped = Math.max(EXPANDED_TOP, Math.min(maxTranslate, raw));

      currentTranslateRef.current = clamped;

      if (rafRef.current) cancelAnimationFrame(rafRef.current);
      rafRef.current = requestAnimationFrame(() => {
        setTranslateY(clamped);
      });
    },
    [getMaxTranslate],
  );

  const recordVelocity = useCallback((clientY: number) => {
    const now = performance.now();
    velocityRef.current.push({ y: clientY, t: now });
    if (velocityRef.current.length > 5) velocityRef.current.shift();
  }, []);

  const getVelocity = useCallback(() => {
    const samples = velocityRef.current;
    if (samples.length < 2) return 0;
    const first = samples[0];
    const last = samples[samples.length - 1];
    const dt = last.t - first.t;
    if (dt < 1) return 0;
    return (first.y - last.y) / dt;
  }, []);

  const toggle = useCallback(() => {
    onSheetStateChange(sheetState === 'collapsed' ? 'expanded' : 'collapsed');
  }, [onSheetStateChange, sheetState]);

  const endDrag = useCallback(() => {
    setIsDragging(false);
    transitionEnabledRef.current = true;
    velocityRef.current = [];
    if (rafRef.current) cancelAnimationFrame(rafRef.current);

    const totalDrag = totalDragRef.current;

    console.log('[sheet] endDrag', {
      hasMoved: hasMovedRef.current,
      totalDrag,
    });

    if (hasMovedRef.current || totalDrag === 0) {
      toggle();
    }
  }, [toggle]);

  const startDrag = useCallback(
    (clientY: number) => {
      console.log('[sheet] startDrag', {
        clientY,
        translateY,
        startTranslateRef: translateY,
      });
      setIsDragging(true);
      hasMovedRef.current = false;
      totalDragRef.current = 0;
      startYRef.current = clientY;
      startTranslateRef.current = translateY;
      currentTranslateRef.current = translateY;
      velocityRef.current = [];
    },
    [translateY],
  );

  const handlePointerDown = useCallback(
    (clientY: number) => {
      startDrag(clientY);
    },
    [startDrag],
  );

  const handlePointerMove = useCallback(
    (clientY: number) => {
      const dy = Math.abs(clientY - startYRef.current);
      totalDragRef.current = Math.max(totalDragRef.current, dy);
      if (dy > TAP_MOVE_THRESHOLD) {
        hasMovedRef.current = true;
      }
      clampTranslate(clientY);
      recordVelocity(clientY);
    },
    [clampTranslate, recordVelocity],
  );

  const handlePointerUp = useCallback(() => {
    endDrag();
  }, [endDrag]);

  const handlePointerMoveRef = useRef(handlePointerMove);
  handlePointerMoveRef.current = handlePointerMove;
  const handlePointerUpRef = useRef(handlePointerUp);
  handlePointerUpRef.current = handlePointerUp;

  useEffect(() => {
    endDragRef.current = endDrag;
    handlePointerDownRef.current = handlePointerDown;
    clampTranslateRef.current = clampTranslate;
    recordVelocityRef.current = recordVelocity;
  });

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      if (touchInProgressRef.current) return;
      handlePointerDown(e.clientY);

      const onMouseMove = (e: MouseEvent) => {
        e.preventDefault();
        handlePointerMoveRef.current(e.clientY);
      };
      const onMouseUp = () => {
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
        handlePointerUpRef.current();
      };
      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);
    },
    [handlePointerDown],
  );

  const filteredEvents = useMemo(
    () => events.filter((e) => enabledEventTypes.includes(e.type)),
    [events, enabledEventTypes],
  );

  const eventsByVenueId = useMemo(() => {
    const grouped: Record<string, EventMapItem[]> = {};
    for (const ev of filteredEvents) {
      if (!grouped[ev.venueId]) grouped[ev.venueId] = [];
      grouped[ev.venueId].push(ev);
    }
    return grouped;
  }, [filteredEvents]);

  const collapsedItems = useMemo(() => {
    const eventSlots = filteredEvents.slice(0, 2);
    const venueSlots =
      eventSlots.length < 2 ? venues.slice(0, 2 - eventSlots.length) : [];
    return { events: eventSlots, venues: venueSlots };
  }, [filteredEvents, venues]);

  const venueLookup = useMemo(
    () =>
      venues.reduce<
        Record<
          string,
          { name: string; address: string; latitude: number; longitude: number }
        >
      >((acc, v) => {
        acc[v.id] = {
          name: v.companyName,
          address: v.address,
          latitude: v.latitude,
          longitude: v.longitude,
        };
        return acc;
      }, {}),
    [venues],
  );

  const handleVenueClick = useCallback(
    (id: string) => {
      const venue = venues.find((v) => v.id === id);
      if (venue) onFlyTo?.(venue.latitude, venue.longitude, venue);
      onSheetStateChange('collapsed');
    },
    [venues, onFlyTo],
  );

  const handleEventClick = useCallback(
    (eventId: string) => {
      const event = filteredEvents.find((e) => e.id === eventId);
      if (event) {
        const venue = venues.find((v) => v.id === event.venueId);
        if (!venue) return;
        onFlyTo?.(venue.latitude, venue.longitude, venue);
      }
      onSheetStateChange('collapsed');
    },
    [filteredEvents, venues, onFlyTo],
  );

  const handleMenuCenter = useCallback(() => {
    if (!menuTarget) return;
    onCenter?.(menuTarget.lat, menuTarget.lng, menuTarget.venue);
    setMenuTarget(null);
    onSheetStateChange('collapsed');
  }, [menuTarget, onCenter]);

  const handleMenuJoinLeave = useCallback(() => {
    if (!menuTarget || !menuTarget.eventId) return;
    if (menuTarget.isJoined) {
      onLeaveEvent?.(menuTarget.eventId);
    } else {
      onJoinEvent?.(menuTarget.eventId);
    }
    setMenuTarget(null);
  }, [menuTarget, onJoinEvent, onLeaveEvent]);

  useEffect(() => {
    const el = dragAreaRef.current;
    if (!el) return;

    const onTouchStart = (e: TouchEvent) => {
      e.preventDefault();
      touchInProgressRef.current = true;
      handlePointerDownRef.current(e.touches[0].clientY);

      const onEnd = () => {
        window.removeEventListener('touchend', onEnd);
        window.removeEventListener('touchcancel', onEnd);
        endDragRef.current();
        setTimeout(() => {
          touchInProgressRef.current = false;
        }, 400);
      };
      window.addEventListener('touchend', onEnd);
      window.addEventListener('touchcancel', onEnd);
    };

    const onTouchMove = (e: TouchEvent) => {
      e.preventDefault();
      const dy = Math.abs(e.touches[0].clientY - startYRef.current);
      totalDragRef.current = Math.max(totalDragRef.current, dy);
      if (dy > TAP_MOVE_THRESHOLD) {
        hasMovedRef.current = true;
      }
      clampTranslateRef.current(e.touches[0].clientY);
      recordVelocityRef.current(e.touches[0].clientY);
    };

    el.addEventListener('touchstart', onTouchStart, { passive: false });
    el.addEventListener('touchmove', onTouchMove, { passive: false });
    return () => {
      el.removeEventListener('touchstart', onTouchStart);
      el.removeEventListener('touchmove', onTouchMove);
    };
  }, []);

  return (
    <>
      <div
        ref={sheetRef}
        className={`${styles.sheet} ${isDragging ? styles.sheetDragging : ''}`}
        style={{
          transform: `translateY(${translateY}px)`,
          transition: transitionEnabledRef.current
            ? 'transform 0.35s cubic-bezier(0.32, 0.72, 0, 1)'
            : 'none',
          overscrollBehavior: 'contain',
        }}
      >
        <div className={styles.fab}>
          <button
            className={styles.fabButton}
            onClick={onAddMeetup}
            aria-label="Add meetup"
          >
            <Plus className="h-6 w-6" />
          </button>
        </div>

        <div
          ref={dragAreaRef}
          className={styles.dragArea}
          onMouseDown={handleMouseDown}
        >
          <div className={styles.dragHandle}>
            <div className={styles.dragHandleBar} />
          </div>

          {sheetState === 'expanded' ? (
            <div className="my-2 flex flex-col gap-2 px-4">
              <div className="flex items-center gap-2">
                <div
                  className={filtersStyles.searchInputContainer}
                  onMouseDown={(e) => e.stopPropagation()}
                >
                  <Search size={16} className="shrink-0 text-zinc-500" />
                  <input
                    type="text"
                    value={inputValue}
                    onChange={handleInputChange}
                    placeholder={tVenues('searchVenues')}
                    className={filtersStyles.searchInput}
                  />
                  {inputValue && (
                    <button
                      type="button"
                      onClick={handleClearInput}
                      className={filtersStyles.clearBtn}
                    >
                      <X size={14} />
                    </button>
                  )}
                </div>
                <button
                  type="button"
                  onClick={() => {
                    setSheetKey((k) => k + 1);
                    setIsFilterSheetOpen(true);
                  }}
                  className={filtersStyles.tagBtn}
                  onMouseDown={(e) => e.stopPropagation()}
                >
                  <ListFilter size={18} className="text-foreground" />
                  {selectedTagIds.length > 0 && (
                    <span className={filtersStyles.tagBtnBadge}>
                      {selectedTagIds.length}
                    </span>
                  )}
                </button>
              </div>

              <div
                className="flex flex-wrap items-center gap-1"
                onMouseDown={(e) => e.stopPropagation()}
              >
                <button
                  onClick={() => onToggleEventType('Event')}
                  className={`rounded-full border px-2.5 py-0.5 text-xs font-medium transition-colors ${
                    enabledEventTypes.includes('Event')
                      ? 'border-primary bg-primary/10 text-primary'
                      : 'border-white/20 text-white/50'
                  }`}
                >
                  {t('event')}
                </button>
                <button
                  onClick={() => onToggleEventType('Meetup')}
                  className={`rounded-full border px-2.5 py-0.5 text-xs font-medium transition-colors ${
                    enabledEventTypes.includes('Meetup')
                      ? 'border-primary bg-primary/10 text-primary'
                      : 'border-white/20 text-white/50'
                  }`}
                >
                  {t('meetup')}
                </button>
                {hasTagFilters &&
                  selectedTagIds.map((id) => {
                    const cat = getTagCategory(id);
                    return (
                      <span
                        key={id}
                        className={`${filtersStyles.chip} ${filtersStyles.chipSelected}${cat ? ` ${filtersStyles[`chipCat${cat}`]}` : ''}`}
                      >
                        <span className={filtersStyles.chipLabel}>
                          {tagLabelById(id)}
                        </span>
                        <button
                          type="button"
                          onClick={() =>
                            setTagIds(
                              selectedTagIds.filter((tid) => tid !== id),
                            )
                          }
                          className={filtersStyles.chipRemove}
                        >
                          <X size={12} />
                        </button>
                      </span>
                    );
                  })}
                {hasTagFilters && (
                  <button
                    type="button"
                    onClick={handleClearTags}
                    className={filtersStyles.clearAllBtn}
                  >
                    {tVenues('clear')}
                  </button>
                )}
              </div>

              <div
                className={`${styles.sheetHeader} ${styles.sheetHeaderCompact}`}
              >
                <span className={styles.sheetTitle}>
                  {t('venuesCount', { count: venues.length })}
                  {filteredEvents.length > 0 && (
                    <span className="text-white/40">
                      {' '}
                      · {t('eventsCount', { count: filteredEvents.length })}
                    </span>
                  )}
                </span>
              </div>
            </div>
          ) : (
            <div className={styles.sheetHeader}>
              <span className={styles.sheetTitle}>
                {t('venuesCount', { count: venues.length })}
                {filteredEvents.length > 0 && (
                  <span className="text-white/40">
                    {' '}
                    · {t('eventsCount', { count: filteredEvents.length })}
                  </span>
                )}
              </span>
              <div className="flex items-center gap-1">
                {hasActiveFilters && (
                  <div className="relative">
                    <ListFilter size={14} className="text-secondary" />
                    <span className="bg-secondary absolute -top-1 -right-1 flex h-3 w-3 items-center justify-center rounded-full text-[8px] font-bold text-white">
                      {filterCount}
                    </span>
                  </div>
                )}
                <ChevronDown size={18} className={styles.chevron} />
              </div>
            </div>
          )}
        </div>

        <div
          className={
            sheetState === 'collapsed' ? styles.peekWrapper : styles.scrollArea
          }
        >
          {sheetState === 'collapsed' ? (
            collapsedItems.events.length === 0 &&
            collapsedItems.venues.length === 0 ? (
              <p className={styles.emptyText}>{t('nothingFound')}</p>
            ) : (
              <>
                {collapsedItems.events.map((event) => {
                  const v = venueLookup[event.venueId];
                  const formattedDate = new Date(
                    event.startDateTime,
                  ).toLocaleDateString(undefined, {
                    month: 'short',
                    day: 'numeric',
                  });
                  const formattedTime = new Date(
                    event.startDateTime,
                  ).toLocaleTimeString(undefined, {
                    hour: '2-digit',
                    minute: '2-digit',
                  });
                  return (
                    <div
                      key={event.id}
                      className={`${styles.eventCard} ${styles.eventCardCollapsed} ${event.isJoined ? styles.eventCardJoined : ''}`}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleEventClick(event.id);
                      }}
                    >
                      {event.coverImageUrl ? (
                        <img
                          src={event.coverImageUrl}
                          alt={event.title}
                          className={styles.eventCardImage}
                        />
                      ) : (
                        <img
                          src="/assets/event/cover-default.webp"
                          alt={event.title}
                          className={styles.eventCardImage}
                        />
                      )}
                      <div className={styles.eventCardOverlay} />
                      <div className={styles.eventCardContent}>
                        <div className={styles.eventCardInfo}>
                          <div className={styles.eventCardTime}>
                            {formattedDate} {formattedTime}
                          </div>
                          <div className={styles.eventCardTitle}>
                            {event.isJoined && (
                              <CheckCircle
                                size={14}
                                className="text-accent shrink-0"
                              />
                            )}
                            <span className="min-w-0 truncate">
                              {event.title}
                            </span>
                          </div>
                        </div>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            setMenuTarget({
                              lat: v?.latitude ?? 0,
                              lng: v?.longitude ?? 0,
                              eventId: event.id,
                              isJoined: event.isJoined,
                            });
                          }}
                          className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full hover:bg-white/10"
                        >
                          <MoreHorizontal size={16} className="text-white/70" />
                        </button>
                      </div>
                    </div>
                  );
                })}
                {collapsedItems.venues.map((venue) => (
                  <div
                    key={venue.id}
                    className={styles.compactCard}
                    onClick={(e) => {
                      e.stopPropagation();
                      handleVenueClick(venue.id);
                    }}
                  >
                    <Image
                      src={
                        venue.companyAvatarImageUrl ||
                        '/assets/common/venues-icon.svg'
                      }
                      alt={venue.companyName}
                      width={40}
                      height={40}
                      className={styles.compactCardAvatar}
                    />
                    <div className={styles.compactCardInfo}>
                      <span className={styles.compactCardName}>
                        {venue.companyName}
                      </span>
                      <span className={styles.compactCardAddress}>
                        {venue.address}
                      </span>
                    </div>
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        setMenuTarget({
                          lat: venue.latitude,
                          lng: venue.longitude,
                          venue,
                        });
                      }}
                      className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full hover:bg-white/10"
                    >
                      <MoreHorizontal size={16} className="text-white/70" />
                    </button>
                  </div>
                ))}
              </>
            )
          ) : venues.length === 0 ? (
            <p className={styles.emptyText}>{t('nothingFound')}</p>
          ) : (
            venues.map((venue) => {
              const venueEvents = eventsByVenueId[venue.id] || [];
              return (
                <div key={venue.id} className="flex flex-col gap-1">
                  <ListItem
                    className="cursor-pointer"
                    onClick={() => handleVenueClick(venue.id)}
                    avatarUrl={
                      venue.companyAvatarImageUrl ||
                      '/assets/common/venues-icon.svg'
                    }
                    avatarAlt={venue.companyName}
                    title={venue.companyName}
                    subtitle={
                      <div>
                        <div className="text-xs text-white/60">
                          {venue.address}
                        </div>
                        <VenueDisplayTags tagIds={venue.tagIds} />
                      </div>
                    }
                    rightEl={
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          setMenuTarget({
                            lat: venue.latitude,
                            lng: venue.longitude,
                            venue,
                          });
                        }}
                        className="flex h-7 w-7 items-center justify-center rounded-full hover:bg-white/10"
                      >
                        <MoreHorizontal size={16} className="text-white/70" />
                      </button>
                    }
                  />
                  {venueEvents.map((event) => {
                    const formattedDate = new Date(
                      event.startDateTime,
                    ).toLocaleDateString(undefined, {
                      month: 'short',
                      day: 'numeric',
                    });
                    const formattedTime = new Date(
                      event.startDateTime,
                    ).toLocaleTimeString(undefined, {
                      hour: '2-digit',
                      minute: '2-digit',
                    });
                    return (
                      <div
                        key={event.id}
                        className={`${styles.eventCard} ${styles.eventCardNested} ${event.isJoined ? styles.eventCardJoined : ''}`}
                        onClick={() => handleEventClick(event.id)}
                      >
                        {event.coverImageUrl ? (
                          <img
                            src={event.coverImageUrl}
                            alt={event.title}
                            className={styles.eventCardImage}
                          />
                        ) : (
                          <img
                            src="/assets/event/cover-default.webp"
                            alt={event.title}
                            className={styles.eventCardImage}
                          />
                        )}
                        <div className={styles.eventCardOverlay} />
                        <div className={styles.eventCardContent}>
                          <div className={styles.eventCardInfo}>
                            <div className={styles.eventCardTime}>
                              {formattedDate} {formattedTime}
                            </div>
                            <div className={styles.eventCardTitle}>
                              {event.isJoined && (
                                <CheckCircle
                                  size={14}
                                  className="text-accent shrink-0"
                                />
                              )}
                              <span className="min-w-0 truncate">
                                {event.title}
                              </span>
                            </div>
                          </div>
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              setMenuTarget({
                                lat: venue.latitude,
                                lng: venue.longitude,
                                eventId: event.id,
                                isJoined: event.isJoined,
                              });
                            }}
                            className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full hover:bg-white/10"
                          >
                            <MoreHorizontal
                              size={16}
                              className="text-white/70"
                            />
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              );
            })
          )}
        </div>
      </div>

      <ModalMenu
        isOpen={!!menuTarget}
        onClose={() => setMenuTarget(null)}
        options={(() => {
          const items = [
            {
              label: t('menu.center'),
              onClick: handleMenuCenter,
            },
          ];
          if (menuTarget?.eventId) {
            items.push({
              label: menuTarget.isJoined ? t('menu.leave') : t('menu.join'),
              onClick: handleMenuJoinLeave,
            });
          }
          return items;
        })()}
      />

      <VenueTagSheet
        key={sheetKey}
        isOpen={isFilterSheetOpen}
        onClose={() => setIsFilterSheetOpen(false)}
      />
    </>
  );
}
