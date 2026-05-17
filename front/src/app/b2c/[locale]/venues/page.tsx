'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { Link, useRouter } from '@/i18n/routing';
import Image from 'next/image';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Spinner } from '@/components/ui/spinner';
import { ListItem } from '@/components/ui/list-item';
import { useInView } from 'react-intersection-observer';
import { useViewportItemCount } from '@/hooks/use-viewport-item-count';
import { useVenuesStore, VenueListItemDto } from './use-venues-store';
import { useVenueFiltersStore } from '@/store/use-venue-filters-store';
import { VenueFilters } from '@/components/b2c/venue-filters';
import { toast } from 'react-toastify';
import { PAGE_ROUTES } from '@/lib/routes';
import {
  useSettingsStore,
  DistanceUnits,
} from '@/app/b2c/[locale]/settings/use-settings-store';
import { metersToKilometers, metersToMiles } from '@/lib/distance-utils';
import { getLowAccuracyLocation } from '@/lib/geo-utils';
import { CheckInManager } from '@/components/common/check-in-manager';
import { useQrCheckIn } from './use-qr-check-in';
import { ScanQrCode, MapPinned } from 'lucide-react';
import styles from './venues.module.css';
import { useSafeBack } from '@/hooks/use-safe-back';
import { City, BELGRADE_COORDS } from '@/lib/cities';
import { CitySelector } from '@/components/common/city-selector';
import { checkPermissionStatus } from '@/lib/permissions';
import { useCitySelection } from '@/hooks/use-city-selection';

export default function VenuesScreen() {
  const router = useRouter();
  const t = useTranslations('screens.venues');
  const tCommon = useTranslations('common');
  const tCities = useTranslations('common.cities');

  const {
    selectedCity,
    hasHydrated,
    handleCityChange: setCity,
  } = useCitySelection();

  const [selectedVenue, setSelectedVenue] = useState<VenueListItemDto | null>(
    null,
  );
  const [isLocationDenied, setIsLocationDenied] = useState(false);
  const [isGettingLocation, setIsGettingLocation] = useState(false);
  const handleCheckInSuccess = useCallback(
    (id: string) => {
      router.push(`${PAGE_ROUTES.VENUE_DETAIL(id)}?animate-check-in=true`);
    },
    [router],
  );

  const handleCheckInError = useCallback(
    (venueId: string | null) => {
      setIsScannerOpen(false);
      if (venueId) {
        router.push(PAGE_ROUTES.VENUE_DETAIL(venueId));
      }
    },
    [router],
  );

  const [isScannerOpen, setIsScannerOpen] = useState(false);
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
  const distanceUnits = useSettingsStore((state) => state.distanceUnits);
  const { safeBack } = useSafeBack();
  const { searchQuery, selectedTagIds } = useVenueFiltersStore();

  const { ref: pageBottomRef, inView } = useInView({
    threshold: 0,
    rootMargin: '200px',
  });

  const limit = useViewportItemCount({
    itemHeight: 88, // Based on .venueItem padding and content
    reservedHeight: 60,
    buffer: 4,
  });

  const { venues, isLoading, hasMore, hasError, fetchVenues, reset } =
    useVenuesStore();

  useEffect(() => {
    const check = async () => {
      const { locationAllowed } = await checkPermissionStatus();
      setIsLocationDenied(!locationAllowed);
    };
    check();

    window.addEventListener('focus', check);
    return () => {
      window.removeEventListener('focus', check);
      reset();
    };
  }, [reset]);

  // Fallback to Belgrade if Nearby is selected but location is denied
  useEffect(() => {
    if (hasHydrated && isLocationDenied && !selectedCity) {
      const belgrade = {
        ...BELGRADE_COORDS,
        label: tCities('Belgrade'),
      };
      setCity(belgrade);
      loadData(0, belgrade);
    }
  }, [hasHydrated, isLocationDenied, selectedCity, setCity, tCities]);

  const isFilterInitial = useRef(true);

  // Re-fetch when filters change (skip initial mount — init effect handles that)
  useEffect(() => {
    if (isFilterInitial.current) {
      isFilterInitial.current = false;
      return;
    }
    reset();
    loadData(0, selectedCity);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchQuery, selectedTagIds]);

  const loadData = async (offset: number, cityOverride?: City | null) => {
    setIsGettingLocation(true);
    const city = cityOverride !== undefined ? cityOverride : selectedCity;

    try {
      let lat, lon;
      if (city) {
        lat = city.latitude;
        lon = city.longitude;
        // as we don't perform location request explicitly, we need to check permission status
        // to handle "Don't Allow" case properly
        const { locationAllowed } = await checkPermissionStatus();
        setIsLocationDenied(!locationAllowed);
      } else {
        const coords = await getLowAccuracyLocation();
        lat = coords.latitude;
        lon = coords.longitude;
      }
      await fetchVenues(
        lat,
        lon,
        offset,
        limit,
        selectedTagIds,
        searchQuery || undefined,
      );
    } catch (err: any) {
      if (err.code === 1) {
        // PERMISSION_DENIED
        setIsLocationDenied(true);
        // If "Nearby" (selectedCity === null) failed, switch to Belgrade
        if (!city) {
          const belgrade = {
            ...BELGRADE_COORDS,
            label: tCities('Belgrade'),
          };
          setCity(belgrade);
          loadData(0, belgrade);
        }
      } else {
        toast.error(t('toast.loadFailed'));
      }
    } finally {
      setIsGettingLocation(false);
    }
  };

  useEffect(() => {
    const init = async () => {
      if (limit === 0 || !hasHydrated) return;

      // If we already have venues, we don't need to re-init unless we want to refresh
      if (venues.length === 0 && !isLoading && !hasError) {
        await loadData(0, selectedCity);
      }
    };
    init();

    window.addEventListener('focus', init);
    return () => window.removeEventListener('focus', init);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [limit, venues.length, selectedCity, hasHydrated]); // Fetch when limit is calculated, list is reset, city changes, or recovered

  useEffect(() => {
    if (
      inView &&
      hasMore &&
      !isLoading &&
      !hasError &&
      venues.length > 0 &&
      hasHydrated
    ) {
      loadData(venues.length, selectedCity);
    }
  }, [
    inView,
    hasMore,
    isLoading,
    hasError,
    venues.length,
    selectedCity,
    hasHydrated,
  ]);

  const getMenuOptions = (venue: VenueListItemDto) => [
    {
      icon: '/assets/venue/venue-icon.svg',
      label: t('menu.viewVenue'),
      onClick: () => {
        router.push(PAGE_ROUTES.VENUE_DETAIL(venue.id));
        setSelectedVenue(null);
      },
    },
    {
      icon: '/assets/venue/google-maps-icon.svg',
      label: t('menu.openInMaps'),
      onClick: () => {
        window.open(venue.googleMapsUrl, '_blank');
        setSelectedVenue(null);
      },
    },
  ];

  const onCityChange = (city: City | null) => {
    setCity(city);
    reset();
    loadData(0, city);
  };

  return (
    <div className="flex flex-1 flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => safeBack(PAGE_ROUTES.FEED)}
            priority
          />
        }
        title={
          <CitySelector
            selectedCity={selectedCity}
            onCityChange={onCityChange}
            isLocationDenied={isLocationDenied}
          />
        }
        right={
          <div className="flex items-center gap-3">
            <button
              onClick={() => setIsScannerOpen(true)}
              style={{
                background: 'none',
                border: 'none',
                padding: 0,
                cursor: 'pointer',
              }}
            >
              <ScanQrCode size={24} className="text-foreground" />
            </button>
            <button
              onClick={() => router.push(`${PAGE_ROUTES.VENUES_MAP}?fresh`)}
              style={{
                background: 'none',
                border: 'none',
                padding: 0,
                cursor: 'pointer',
              }}
            >
              <MapPinned size={24} className="text-foreground" />
            </button>
          </div>
        }
      />

      <VenueFilters />

      <div className={styles.content}>
        {(isLoading || isGettingLocation) && venues.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : isLocationDenied && !selectedCity ? (
          <div className={styles.emptyState}>
            <Image
              src="/assets/venue/geolocation-disabled-placeholder.webp"
              alt="location required"
              width={300}
              height={300}
            />
            <span className={styles.emptyText}>
              {t.rich('locationRequired', {
                link: (chunks) => (
                  <Link
                    href={PAGE_ROUTES.PERMISSIONS}
                    className={styles.emptyStateLink}
                  >
                    {chunks}
                  </Link>
                ),
              })}
            </span>
          </div>
        ) : venues.length === 0 &&
          !isLoading &&
          !(searchQuery || selectedTagIds.length > 0) ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyText}>{t('noVenues')}</span>
          </div>
        ) : venues.length === 0 &&
          !isLoading &&
          (searchQuery || selectedTagIds.length > 0) ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyText}>{t('noVenuesFiltered')}</span>
          </div>
        ) : (
          <div className={styles.venueList}>
            {venues.map((venue) => (
              <ListItem
                key={venue.id}
                className="cursor-pointer"
                onClick={() => setSelectedVenue(venue)}
                avatarUrl={
                  venue.companyAvatarImageUrl ||
                  '/assets/common/venues-icon.svg'
                }
                avatarAlt={venue.companyName}
                title={venue.companyName}
                subtitle={
                  <>
                    {!selectedCity && (
                      <>
                        <span>
                          {t('distance', {
                            distance:
                              distanceUnits === DistanceUnits.Miles
                                ? metersToMiles(venue.distanceMeters)
                                : metersToKilometers(venue.distanceMeters),
                            units:
                              distanceUnits === DistanceUnits.Miles
                                ? tCommon('units.miles')
                                : tCommon('units.kilometers'),
                          })}
                        </span>
                        <span>•</span>
                      </>
                    )}
                    <span>{venue.address}</span>
                  </>
                }
                rightEl={
                  <Icon
                    src="/assets/common/more-icon.svg"
                    alt="more"
                    width={24}
                    height={24}
                    priority
                    stopPropagation
                    onClick={() => setSelectedVenue(venue)}
                  />
                }
              />
            ))}
            {hasMore && (
              <div ref={pageBottomRef} className="flex justify-center py-4">
                <Spinner />
              </div>
            )}
          </div>
        )}
      </div>

      <ModalMenu
        isOpen={!!selectedVenue}
        onClose={() => setSelectedVenue(null)}
        options={selectedVenue ? getMenuOptions(selectedVenue) : []}
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
