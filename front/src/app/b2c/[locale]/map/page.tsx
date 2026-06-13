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
import { VenueSelectorSheet } from './_components/venue-selector-sheet';
import { useMapStore } from './use-map-store';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { toast } from 'react-toastify';
import styles from './map.module.css';
import type { VenueMapItem } from '../venues/map/_components/venue-map-internal';
import type { EventCountMap } from './_components/types';

const VenueMapInternal = dynamic(
  () => import('../venues/map/_components/venue-map-internal'),
  {
    ssr: false,
    loading: () => (
      <div className={styles.mapLoading}>
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
    clearFocus,
    flyTo,
    updateEventParticipation,
    deleteEvent,
  } = useMapStore();

  const lastBoundsRef = useRef<string | null>(null);
  const currentBoundsRef = useRef<{
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  } | null>(null);

  const { searchQuery, selectedTagIds } = useVenueFiltersStore();
  const focusSuppressUntilRef = useRef(0);

  useEffect(() => {
    setFlyToCoord(null);
    clearFocus();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const mapCenter = useMemo(
    () => ({
      lat: selectedCity?.latitude ?? BELGRADE_COORDS.latitude,
      lng: selectedCity?.longitude ?? BELGRADE_COORDS.longitude,
    }),
    [selectedCity?.latitude, selectedCity?.longitude],
  );

  const [enabledEventTypes, setEnabledEventTypes] = useState<
    ('Event' | 'Meetup')[]
  >(['Event', 'Meetup']);
  const toggleEventType = useCallback((type: 'Event' | 'Meetup') => {
    setEnabledEventTypes((prev) =>
      prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type],
    );
  }, []);

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
      if (Date.now() < focusSuppressUntilRef.current) return;
      const boundsKey = await fetchMapData(
        bounds,
        searchQuery,
        selectedTagIds,
        enabledEventTypes,
      );
      if (boundsKey !== null) lastBoundsRef.current = boundsKey;
    },
    [fetchMapData, searchQuery, selectedTagIds, enabledEventTypes],
  );

  useEffect(() => {
    if (currentBoundsRef.current) {
      lastBoundsRef.current = null;
      const bounds = currentBoundsRef.current;
      fetchMapDataRef.current(
        bounds,
        searchQuery,
        selectedTagIds,
        enabledEventTypes,
      );
    }
  }, [searchQuery, selectedTagIds, enabledEventTypes]);

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

  const handleFlyTo = useCallback(
    (lat: number, lng: number, venue?: VenueMapItem) => {
      focusSuppressUntilRef.current = Date.now() + 4000;
      if (venue) {
        setFocusedVenue(null);
        requestAnimationFrame(() => setFocusedVenue(venue));
      }
    },
    [setFocusedVenue],
  );

  const CENTER_FLY_SUPPRESS_MS = 2000;

  const handleCenter = useCallback(
    (lat: number, lng: number, venue?: VenueMapItem) => {
      focusSuppressUntilRef.current = Date.now() + CENTER_FLY_SUPPRESS_MS;
      flyTo(lat, lng, venue);
      setTimeout(() => {
        focusSuppressUntilRef.current = 0;
        if (currentBoundsRef.current) {
          fetchMapData(
            currentBoundsRef.current,
            searchQuery,
            selectedTagIds,
            enabledEventTypes,
          );
        }
      }, CENTER_FLY_SUPPRESS_MS + 100);
    },
    [flyTo, fetchMapData, searchQuery, selectedTagIds, enabledEventTypes],
  );

  const handleJoinEvent = useCallback(
    async (eventId: string) => {
      try {
        await apiClient.post(API_ENDPOINTS.EVENTS.JOIN(eventId));
        if (currentBoundsRef.current) {
          await fetchMapData(
            currentBoundsRef.current,
            searchQuery,
            selectedTagIds,
            enabledEventTypes,
          );
        }
      } catch {
        toast.error('Failed to join event');
      }
    },
    [fetchMapData, searchQuery, selectedTagIds, enabledEventTypes],
  );

  const handleLeaveEvent = useCallback(
    async (eventId: string) => {
      try {
        await apiClient.post(API_ENDPOINTS.EVENTS.LEAVE(eventId));
        if (currentBoundsRef.current) {
          await fetchMapData(
            currentBoundsRef.current,
            searchQuery,
            selectedTagIds,
            enabledEventTypes,
          );
        }
      } catch {
        toast.error('Failed to leave event');
      }
    },
    [fetchMapData, searchQuery, selectedTagIds, enabledEventTypes],
  );

  const handleDeleteEvent = useCallback(
    async (eventId: string) => {
      try {
        await deleteEvent(eventId);
      } catch {
        toast.error(t('toast.loadFailed'));
      }
    },
    [deleteEvent, t],
  );

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

  const [sheetState, setSheetState] = useState<'collapsed' | 'expanded'>(
    'collapsed',
  );

  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const [isFirstCheckInDialogOpen, setIsFirstCheckInDialogOpen] =
    useState(false);
  const [isVenueSelectorOpen, setIsVenueSelectorOpen] = useState(false);

  const handleAddMeetupClick = useCallback(() => {
    setIsVenueSelectorOpen(true);
  }, []);

  const handleSelectVenue = useCallback(
    (venueId: string, _venueName: string) => {
      router.push(PAGE_ROUTES.MEETUP_CREATE(venueId));
    },
    [router],
  );

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
        <div className={styles.mapLoadingScreen}>
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
          onBoundsChange={handleBoundsChange}
          onMapDragStart={() => setSheetState('collapsed')}
          onVenueDetails={handleVenueDetails}
          onVenueEvents={handleVenueEvents}
          detailsLabel={t('details')}
          eventsLabel={t('eventsLabel')}
          initialZoom={12}
        />

        <div className={styles.leftControls}>
          <div className={styles.profileBtn}>
            <Icon
              src="/assets/common/profile-icon.svg"
              alt="profile"
              width={30}
              height={32}
              priority
              onClick={() => router.push(PAGE_ROUTES.PROFILE)}
            />
          </div>
          <MapFeedToggle activeView="map" />
        </div>

        <div className={styles.topBar}>
          <div className={styles.topBarItem} /> {/* spacer to preserve layout balance */}

          <div className={styles.topBarPill}>
            <CitySelector
              selectedCity={selectedCity}
              onCityChange={setCity}
              isLocationDenied={false}
            />
          </div>

          <div className={styles.topBarIconPill}>
            <div className={styles.topBarIcons}>
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

        {isLoading && (
          <div className={styles.topRightControls}>
            <div className={styles.loadingIndicator}>
              <div className={styles.smallSpinner} />
            </div>
          </div>
        )}

        <VenueBottomSheet
          venues={venues}
          events={allEvents}
          isLoading={isLoading}
          onFlyTo={handleFlyTo}
          onCenter={handleCenter}
          onJoinEvent={handleJoinEvent}
          onLeaveEvent={handleLeaveEvent}
          onDeleteEvent={handleDeleteEvent}
          sheetState={sheetState}
          onSheetStateChange={setSheetState}
          enabledEventTypes={enabledEventTypes}
          onToggleEventType={toggleEventType}
          onAddMeetup={handleAddMeetupClick}
        />
      </div>

      <VenueSelectorSheet
        isOpen={isVenueSelectorOpen}
        onClose={() => setIsVenueSelectorOpen(false)}
        onSelectVenue={handleSelectVenue}
        initialVenues={venues}
      />

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
