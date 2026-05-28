'use client';

import { useRef, useState, useCallback, useEffect, useLayoutEffect } from 'react';
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
const VELOCITY_THRESHOLD = 0.3;
const TAP_MOVE_THRESHOLD = 8;

interface VenueBottomSheetProps {
  venues: VenueMapItem[];
  isLoading: boolean;
}

type SheetState = 'collapsed' | 'expanded';

export function VenueBottomSheet({ venues, isLoading }: VenueBottomSheetProps) {
  const t = useTranslations('screens.map');
  const router = useRouter();

  const [state, setState] = useState<SheetState>('collapsed');
  const [translateY, setTranslateY] = useState(() => {
    if (typeof window === 'undefined') return 0;
    return Math.max(window.innerHeight - 60 - PEEK_HEIGHT, EXPANDED_TOP);
  });
  const [isDragging, setIsDragging] = useState(false);

  const sheetRef = useRef<HTMLDivElement>(null);
  const startYRef = useRef(0);
  const startTranslateRef = useRef(0);
  const rafRef = useRef<number | null>(null);
  const velocityRef = useRef<{ y: number; t: number }[]>([]);
  const hasMovedRef = useRef(false);
  const transitionEnabledRef = useRef(false);

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

  const clampTranslate = useCallback((clientY: number) => {
    const dy = clientY - startYRef.current;
    const raw = startTranslateRef.current + dy;
    const maxTranslate = getMaxTranslate();
    const clamped = Math.max(EXPANDED_TOP, Math.min(maxTranslate, raw));

    if (rafRef.current) cancelAnimationFrame(rafRef.current);
    rafRef.current = requestAnimationFrame(() => {
      setTranslateY(clamped);
    });
  }, [getMaxTranslate]);

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

    if (!hasMovedRef.current) {
      toggle();
      return;
    }

    const maxTranslate = getMaxTranslate();
    const midPoint = (maxTranslate + EXPANDED_TOP) / 2;
    const velocity = getVelocity();

    let next: SheetState;
    if (Math.abs(velocity) > VELOCITY_THRESHOLD) {
      next = velocity > 0 ? 'collapsed' : 'expanded';
    } else {
      next = translateY < midPoint ? 'expanded' : 'collapsed';
    }
    setState(next);
  }, [translateY, getMaxTranslate, getVelocity, toggle]);

  const startDrag = useCallback((clientY: number) => {
    setIsDragging(true);
    hasMovedRef.current = false;
    startYRef.current = clientY;
    startTranslateRef.current = translateY;
    velocityRef.current = [];
  }, [translateY]);

  const handlePointerDown = useCallback((clientY: number) => {
    startDrag(clientY);
  }, [startDrag]);

  const handlePointerMove = useCallback((clientY: number) => {
    if (!isDragging) return;
    if (Math.abs(clientY - startYRef.current) > TAP_MOVE_THRESHOLD) {
      hasMovedRef.current = true;
    }
    if (!hasMovedRef.current) return;
    clampTranslate(clientY);
    recordVelocity(clientY);
  }, [isDragging, clampTranslate, recordVelocity]);

  const handlePointerUp = useCallback(() => {
    if (!isDragging) return;
    endDrag();
  }, [isDragging, endDrag]);

  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    handlePointerDown(e.touches[0].clientY);
  }, [handlePointerDown]);

  const handleTouchMove = useCallback((e: React.TouchEvent) => {
    e.preventDefault();
    handlePointerMove(e.touches[0].clientY);
  }, [handlePointerMove]);

  const handleTouchEnd = useCallback(() => {
    handlePointerUp();
  }, [handlePointerUp]);

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    handlePointerDown(e.clientY);
  }, [handlePointerDown]);

  useEffect(() => {
    if (!isDragging) return;

    const onMouseMove = (e: MouseEvent) => {
      e.preventDefault();
      handlePointerMove(e.clientY);
    };
    const onMouseUp = () => {
      handlePointerUp();
    };

    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);
    return () => {
      window.removeEventListener('mousemove', onMouseMove);
      window.removeEventListener('mouseup', onMouseUp);
    };
  }, [isDragging, handlePointerMove, handlePointerUp]);

  const handleVenueClick = useCallback(
    (id: string) => router.push(PAGE_ROUTES.VENUE_DETAIL(id)),
    [router],
  );

  return (
    <div
      ref={sheetRef}
      className={`${styles.sheet} ${isDragging ? styles.sheetDragging : ''}`}
      style={{
        transform: `translateY(${translateY}px)`,
        transition: transitionEnabledRef.current
          ? 'transform 0.35s cubic-bezier(0.32, 0.72, 0, 1)'
          : 'none',
      }}
    >
      <div
        className={styles.dragArea}
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
        onMouseDown={handleMouseDown}
      >
        <div className={styles.dragHandle}>
          <div className={styles.dragHandleBar} />
        </div>

        <div className={styles.sheetHeader}>
          <span className={styles.sheetTitle}>
            {t('venuesCount', { count: venues.length })}
          </span>
          <ChevronDown
            size={18}
            className={`${styles.chevron} ${state === 'expanded' ? styles.chevronUp : ''}`}
          />
        </div>
      </div>

      <div className={state === 'collapsed' ? styles.peekWrapper : styles.scrollArea}>
        {venues.length === 0
          ? <p className={styles.emptyText}>{t('nothingFound')}</p>
          : state === 'collapsed'
            ? venues.slice(0, 2).map((venue) => (
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
            : venues.map((venue) => (
                <ListItem
                  key={venue.id}
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
                      <div className="text-xs text-white/60">{venue.address}</div>
                      <VenueDisplayTags tagIds={venue.tagIds} />
                    </div>
                  }
                />
              ))}
      </div>
    </div>
  );
}
