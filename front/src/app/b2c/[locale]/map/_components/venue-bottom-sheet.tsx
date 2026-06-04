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
import { ChevronDown, MoreHorizontal } from 'lucide-react';
import { ListItem } from '@/components/ui/list-item';
import { ModalMenu } from '@/components/ui/modal-menu';
import { VenueDisplayTags } from '@/components/b2c/venue-display-tags';
import styles from '../map.module.css';
import type { VenueMapItem } from '../../venues/map/_components/venue-map-internal';
import type { EventMapItem } from '../_components/types';

const PEEK_HEIGHT = 160;
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
}

type SheetState = 'collapsed' | 'expanded';

type Tab = 'venues' | 'events';

export function VenueBottomSheet({
  venues,
  events,
  isLoading,
  onFlyTo,
  onCenter,
  onJoinEvent,
  onLeaveEvent,
}: VenueBottomSheetProps) {
  const t = useTranslations('screens.map');

  const [state, setState] = useState<SheetState>('collapsed');
  const [tab, setTab] = useState<Tab>('venues');
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
    setTranslateY(getSnapTranslate(state));
    transitionEnabledRef.current = true;
  }, [state, getSnapTranslate, isDragging]);

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
    setState((prev) => (prev === 'collapsed' ? 'expanded' : 'collapsed'));
  }, []);

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

  const venueLookup = useMemo(
    () =>
      venues.reduce<Record<string, { name: string; address: string; latitude: number; longitude: number }>>(
        (acc, v) => {
          acc[v.id] = { name: v.companyName, address: v.address, latitude: v.latitude, longitude: v.longitude };
          return acc;
        },
        {},
      ),
    [venues],
  );

  const handleVenueClick = useCallback(
    (id: string) => {
      const venue = venues.find((v) => v.id === id);
      if (venue) onFlyTo?.(venue.latitude, venue.longitude, venue);
      setState('collapsed');
    },
    [venues, onFlyTo],
  );

  const handleEventClick = useCallback(
    (eventId: string) => {
      const event = events.find((e) => e.id === eventId);
      if (event) {
        const venue = venues.find((v) => v.id === event.venueId);
        if (!venue) return;
        onFlyTo?.(venue.latitude, venue.longitude, venue);
      }
      setState('collapsed');
    },
    [events, venues, onFlyTo],
  );

  const handleMenuCenter = useCallback(() => {
    if (!menuTarget) return;
    onCenter?.(menuTarget.lat, menuTarget.lng, menuTarget.venue);
    setMenuTarget(null);
    setState('collapsed');
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
      <div
        ref={dragAreaRef}
        className={styles.dragArea}
        onMouseDown={handleMouseDown}
        style={{ touchAction: 'none' }}
      >
        <div className={styles.dragHandle}>
          <div className={styles.dragHandleBar} />
        </div>

        <div className={styles.sheetHeader}>
          <span className={styles.sheetTitle}>
            {tab === 'events'
              ? t('eventsCount', { count: events.length })
              : t('venuesCount', { count: venues.length })}
          </span>
          <div className="flex items-center gap-2">
            <div className={styles.tabPillToggle}>
              <button
                className={`${styles.tabPillOption} ${tab === 'venues' ? styles.tabPillOptionActive : ''}`}
                onClick={(e) => {
                  e.stopPropagation();
                  setTab('venues');
                  toggle();
                }}
              >
                {t('tabVenues')}
              </button>
              <button
                className={`${styles.tabPillOption} ${tab === 'events' ? styles.tabPillOptionActive : ''}`}
                onClick={(e) => {
                  e.stopPropagation();
                  setTab('events');
                  toggle();
                }}
              >
                {t('tabEvents')}
              </button>
            </div>
            <ChevronDown
              size={18}
              className={`${styles.chevron} ${state === 'expanded' ? styles.chevronUp : ''}`}
            />
          </div>
        </div>
      </div>

      <div
        className={
          state === 'collapsed' ? styles.peekWrapper : styles.scrollArea
        }
      >
        {tab === 'events' ? (
          events.length === 0 ? (
            <p className={styles.emptyText}>{t('nothingFound')}</p>
          ) : state === 'collapsed' ? (
            events.slice(0, 2).map((event) => {
              const v = venueLookup[event.venueId];
              return (
              <div
                key={event.id}
                className={styles.compactCard}
                onClick={(e) => {
                  e.stopPropagation();
                  handleEventClick(event.id);
                }}
              >
                <div className={styles.eventCompactInfo}>
                  <span className={styles.compactCardName}>{event.title}</span>
                  <span className={styles.eventCompactMeta}>
                    {new Date(event.startDateTime).toLocaleDateString(
                      undefined,
                      {
                        month: 'short',
                        day: 'numeric',
                      },
                    )}{' '}
                    {new Date(event.startDateTime).toLocaleTimeString(
                      undefined,
                      {
                        hour: '2-digit',
                        minute: '2-digit',
                      },
                    )}
                    {' • '}
                    {event.type === 'Meetup' ? t('meetup') : t('event')}
                  </span>
                </div>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    setMenuTarget({ lat: v?.latitude ?? 0, lng: v?.longitude ?? 0, eventId: event.id, isJoined: event.isJoined });
                  }}
                  className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full hover:bg-white/10"
                >
                  <MoreHorizontal size={16} className="text-white/70" />
                </button>
              </div>
              );
            })
          ) : (
            events.map((event) => {
              const venue = venueLookup[event.venueId];
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
                  className={styles.eventCard}
                  onClick={() => handleEventClick(event.id)}
                >
                  {event.coverImageUrl ? (
                    <img
                      src={event.coverImageUrl}
                      alt={event.title}
                      className={styles.eventCardImage}
                    />
                  ) : null}
                  <div className={styles.eventCardOverlay} />
                  <div className={styles.eventCardContent}>
                    <div className={styles.eventCardInfo}>
                      <div className={styles.eventCardTime}>
                        {formattedDate} {formattedTime}
                      </div>
                      <div className={styles.eventCardTitle}>{event.title}</div>
                      <div className={styles.eventCardMeta}>
                        <span>{venue?.address ?? ''}</span>
                        {event.type === 'Meetup' ? (
                          <span className="text-primary">{t('meetup')}</span>
                        ) : (
                          <span className="text-primary">{t('event')}</span>
                        )}
                    </div>
                  </div>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setMenuTarget({ lat: venue?.latitude ?? 0, lng: venue?.longitude ?? 0, eventId: event.id, isJoined: event.isJoined });
                    }}
                    className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full hover:bg-white/10"
                  >
                    <MoreHorizontal size={16} className="text-white/70" />
                  </button>
                </div>
                </div>
              );
            })
          )
        ) : venues.length === 0 ? (
          <p className={styles.emptyText}>{t('nothingFound')}</p>
        ) : state === 'collapsed' ? (
          venues.slice(0, 2).map((venue) => (
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
                setMenuTarget({ lat: venue.latitude, lng: venue.longitude, venue });
              }}
              className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full hover:bg-white/10"
            >
              <MoreHorizontal size={16} className="text-white/70" />
            </button>
          </div>
          ))
        ) : (
          venues.map((venue) => (
            <ListItem
              key={venue.id}
              className="cursor-pointer"
              onClick={() => handleVenueClick(venue.id)}
              avatarUrl={
                venue.companyAvatarImageUrl || '/assets/common/venues-icon.svg'
              }
              avatarAlt={venue.companyName}
              title={venue.companyName}
              subtitle={
                <div>
                  <div className="text-xs text-white/60">{venue.address}</div>
                  <VenueDisplayTags tagIds={venue.tagIds} />
                </div>
              }
              rightEl={
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    setMenuTarget({ lat: venue.latitude, lng: venue.longitude, venue });
                  }}
                  className="flex h-7 w-7 items-center justify-center rounded-full hover:bg-white/10"
                >
                  <MoreHorizontal size={16} className="text-white/70" />
                </button>
              }
            />
          ))
        )}
      </div>
    </div>

    <ModalMenu
      isOpen={!!menuTarget}
      onClose={() => setMenuTarget(null)}
      options={(() => {
        const items = [
          {
            label: 'Center',
            onClick: handleMenuCenter,
          },
        ];
        if (menuTarget?.eventId) {
          items.push({
            label: menuTarget.isJoined ? 'Leave' : 'Join',
            onClick: handleMenuJoinLeave,
          });
        }
        return items;
      })()}
    />
    </>
  );
}
