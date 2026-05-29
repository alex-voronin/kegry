'use client';

import {
  useRef,
  useState,
  useCallback,
  useEffect,
  useLayoutEffect,
} from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import Image from 'next/image';
import { ChevronDown } from 'lucide-react';
import { ListItem } from '@/components/ui/list-item';
import { VenueDisplayTags } from '@/components/b2c/venue-display-tags';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../map.module.css';
import type { VenueMapItem } from '../../venues/map/_components/venue-map-internal';

const PEEK_HEIGHT = 160;
const EXPANDED_TOP = 190;
const TAP_MOVE_THRESHOLD = 50;

interface VenueBottomSheetProps {
  venues: VenueMapItem[];
  isLoading: boolean;
}

type SheetState = 'collapsed' | 'expanded';

type Tab = 'venues' | 'events';

export function VenueBottomSheet({ venues, isLoading }: VenueBottomSheetProps) {
  const t = useTranslations('screens.map');
  const router = useRouter();

  const [state, setState] = useState<SheetState>('collapsed');
  const [tab, setTab] = useState<Tab>('venues');
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

  const handleVenueClick = useCallback(
    (id: string) => router.push(PAGE_ROUTES.VENUE_DETAIL(id)),
    [router],
  );

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
            {t('venuesCount', { count: venues.length })}
          </span>
          <div className="flex items-center gap-2">
            <div className={styles.tabPillToggle}>
              <button
                className={`${styles.tabPillOption} ${tab === 'venues' ? styles.tabPillOptionActive : ''}`}
                onClick={(e) => { e.stopPropagation(); setTab('venues'); toggle(); }}
              >
                {t('tabVenues')}
              </button>
              <button
                className={`${styles.tabPillOption} ${tab === 'events' ? styles.tabPillOptionActive : ''}`}
                onClick={(e) => { e.stopPropagation(); setTab('events'); toggle(); }}
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
          <p className={styles.eventsPlaceholderText}>
            {t('eventsPlaceholder')}
          </p>
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
            />
          ))
        )}
      </div>
    </div>
  );
}
