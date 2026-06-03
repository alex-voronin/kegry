'use client';

import { useState, useCallback, useRef, useEffect, useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { ScanQrCode } from 'lucide-react';
import dynamic from 'next/dynamic';

import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { CheckInManager } from '@/components/common/check-in-manager';
import { PAGE_ROUTES } from '@/lib/routes';
import { useCitySelection } from '@/hooks/use-city-selection';
import { BELGRADE_COORDS } from '@/lib/cities';
import { CitySelector } from '@/components/common/city-selector';
import { useVenueFiltersStore } from '@/store/use-venue-filters-store';
import { MapFeedToggle } from '@/components/common/map-feed-toggle';
import { useQrCheckIn } from '../venues/use-qr-check-in';
import { VenueBottomSheet } from './_components/venue-bottom-sheet';
import { useMapStore } from './use-map-store';
import styles from './map.module.css';
import type { VenueMapItem } from '../venues/map/_components/venue-map-internal';
import type { EventCountMap } from './_components/types';

const VenueMapInternal = dynamic(
  () => import('../venues/map/_components/venue-map-internal'),
  {
    ssr: false,
    loading: () => (
      <div className="flex h-full w-full items-center justify-center bg-[#1A1A1A]">
        <div className="border-primary h-8 w-8 animate-spin rounded-full border-b-2"></div>
      </div>
    ),
  },
);

export default function MapScreen() {
  const router = useRouter();
  const t = useTranslations('screens.map');
  const {
    selectedCity,
    hasHydrated,
    handleCityChange: setCity,
  } = useCitySelection();

  const {
    venues,
    venueEvents,
    isLoading,
    flyToCoord,
    focusedVenue,
    fetchMapData,
    setFlyToCoord,
    setFocusedVenue,
    clearFocus: handleClearFocus,
  } = useMapStore();

  const lastBoundsRef = useRef<string | null>(null);
  const currentBoundsRef = useRef<{
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  } | null>(null);

  const { searchQuery, selectedTagIds } = useVenueFiltersStore();

  const mapCenter = useMemo(
    () => ({
      lat: selectedCity?.latitude ?? BELGRADE_COORDS.latitude,
      lng: selectedCity?.longitude ?? BELGRADE_COORDS.longitude,
    }),
    [selectedCity?.latitude, selectedCity?.longitude],
  );

  const fetchMapDataRef = useRef(fetchMapData);
  fetchMapDataRef.current = fetchMapData;

  const handleBoundsChange = useCallback(
    async (bounds: {
      minLat: number;
      maxLat: number;
      minLng: number;
      maxLng: number;
    }) => {
      currentBoundsRef.current = bounds;
      const boundsKey = await fetchMapData(bounds, searchQuery, selectedTagIds);
      if (boundsKey !== null) lastBoundsRef.current = boundsKey;
    },
    [fetchMapData, searchQuery, selectedTagIds],
  );

  useEffect(() => {
    if (currentBoundsRef.current) {
      lastBoundsRef.current = null;
      const bounds = currentBoundsRef.current;
      fetchMapDataRef.current(bounds, searchQuery, selectedTagIds);
    }
  }, [searchQuery, selectedTagIds]);

  const allEvents = useMemo(
    () => venueEvents.flatMap((v) => v.events),
    [venueEvents],
  );

  const eventCountByVenueId = useMemo<EventCountMap>(() => {
    const counts: EventCountMap = {};
    for (const v of venueEvents) {
      counts[v.venueId] = v.totalCount;
    }
    return counts;
  }, [venueEvents]);

  const flyToKeyRef = useRef(0);

  const handleFlyTo = useCallback((lat: number, lng: number, venue?: VenueMapItem) => {
    flyToKeyRef.current += 1;
    setFlyToCoord({ lat, lng, key: flyToKeyRef.current });
    if (venue) setFocusedVenue(venue);
  }, [setFlyToCoord, setFocusedVenue]);

  const handleVenueDetails = useCallback(
    (id: string) => {
      router.push(PAGE_ROUTES.VENUE_DETAIL(id));
    },
    [router],
  );

  const handleVenueEvents = useCallback(
    (id: string) => {
      router.push(`${PAGE_ROUTES.VENUE_DETAIL(id)}?tab=events`);
    },
    [router],
  );

  // --- QR Check-in ---

  const handleCheckInSuccess = useCallback(
    (venueId: string) => {
      requestAnimationFrame(() => {
        router.push(
          `${PAGE_ROUTES.VENUE_DETAIL(venueId)}?animate-check-in=true`,
        );
      });
    },
    [router],
  );

  const handleCheckInError = useCallback(
    (venueId: string | null) => {
      setIsScannerOpen(false);
      if (venueId) {
        requestAnimationFrame(() => {
          router.push(PAGE_ROUTES.VENUE_DETAIL(venueId));
        });
      } else {
        router.refresh();
      }
    },
    [router],
  );

  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const [isFirstCheckInDialogOpen, setIsFirstCheckInDialogOpen] =
    useState(false);

  const handleQrScanClick = useCallback(() => {
    const hasSeenDialog = localStorage.getItem('has_seen_first_checkin_dialog');
    if (!hasSeenDialog) {
      setIsFirstCheckInDialogOpen(true);
    } else {
      setIsScannerOpen(true);
    }
  }, []);

  const handleFirstCheckInOk = useCallback(() => {
    localStorage.setItem('has_seen_first_checkin_dialog', 'true');
    setIsFirstCheckInDialogOpen(false);
    setIsScannerOpen(true);
  }, []);

  const {
    isCodePopupOpen,
    setIsCodePopupOpen,
    handleQrCode,
    handleCodeSubmit,
    showLocationRequiredDialog,
    dismissLocationRequiredDialog,
  } = useQrCheckIn({
    onSuccess: handleCheckInSuccess,
    onError: handleCheckInError,
  });

  if (!hasHydrated) {
    return (
      <div className={styles.wrapper}>
        <div className="flex h-dvh items-center justify-center">
          <Spinner />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.wrapper}>
      <div className={styles.mapArea}>
        <VenueMapInternal
          center={mapCenter}
          venues={venues}
          eventCountByVenueId={eventCountByVenueId}
          flyToCoord={flyToCoord}
          focusedVenue={focusedVenue}
          onClearFocus={handleClearFocus}
          onBoundsChange={handleBoundsChange}
          onVenueDetails={handleVenueDetails}
          onVenueEvents={handleVenueEvents}
          detailsLabel={t('details')}
          eventsLabel={t('eventsLabel')}
          initialZoom={12}
        />

        <div className="absolute top-2 right-0 left-0 z-50 flex items-center justify-between px-2">
          <div className="bg-surface/85 rounded-full p-2">
            <Icon
              src="/assets/common/profile-icon.svg"
              alt="profile"
              width={30}
              height={32}
              priority
              onClick={() => router.push(PAGE_ROUTES.PROFILE)}
            />
          </div>

          <div className="bg-surface/85 rounded-full px-4 py-2 backdrop-blur-md">
            <CitySelector
              selectedCity={selectedCity}
              onCityChange={setCity}
              isLocationDenied={false}
            />
          </div>

          <div className="bg-surface/85 rounded-full px-3 py-2 backdrop-blur-md">
            <div className="flex items-center gap-2">
              <Icon width={32} height={32} onClick={handleQrScanClick}>
                <ScanQrCode className="text-primary h-full w-full" />
              </Icon>
              <Icon
                src="/assets/venue/venue-icon.svg"
                alt="venues"
                width={30}
                height={30}
                priority
                onClick={() => router.push(PAGE_ROUTES.VENUES)}
              />
              <Icon
                src="/assets/feed/notifications-icon.svg"
                alt="notifications"
                width={28}
                height={30}
                priority
                onClick={() => router.push(PAGE_ROUTES.NOTIFICATIONS)}
              />
            </div>
          </div>
        </div>

        <div className={styles.topRightControls}>
          {isLoading && (
            <div className={styles.loadingSpinner}>
              <div
                className="h-4 w-4 animate-spin rounded-full border-2 border-white/30"
                style={{ borderTopColor: '#fff' }}
              />
            </div>
          )}

          <MapFeedToggle activeView="map" />
        </div>

        <VenueBottomSheet
          venues={venues}
          events={allEvents}
          isLoading={isLoading}
          onFlyTo={handleFlyTo}
        />
      </div>

      <ModalDialog
        isOpen={isFirstCheckInDialogOpen}
        onClose={handleFirstCheckInOk}
        message={t('firstCheckInDialog.message')}
        primaryButtonText={t('firstCheckInDialog.ok')}
        onPrimaryAction={handleFirstCheckInOk}
      />

      <CheckInManager
        isScannerOpen={isScannerOpen}
        onScannerOpenChange={setIsScannerOpen}
        isCodePopupOpen={isCodePopupOpen}
        onCodePopupOpenChange={setIsCodePopupOpen}
        handleQrCode={handleQrCode}
        handleCodeSubmit={handleCodeSubmit}
        showLocationRequiredDialog={showLocationRequiredDialog}
        onDismissLocationRequiredDialog={dismissLocationRequiredDialog}
      />
    </div>
  );
}
