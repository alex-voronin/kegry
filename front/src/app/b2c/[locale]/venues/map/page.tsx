'use client';

import { useState, useCallback, useRef, useEffect, useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSearchParams } from 'next/navigation';
import dynamic from 'next/dynamic';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useCitySelection } from '@/hooks/use-city-selection';
import { BELGRADE_COORDS } from '@/lib/cities';
import apiClient from '@/lib/api-client';
import { paramsForReq } from '@/lib/query-params';
import { useVenueFiltersStore } from '@/store/use-venue-filters-store';
import { VenueFilters } from '@/components/b2c/venue-filters';
import styles from './map.module.css';
import type { VenueMapItem } from './_components/venue-map-internal';
import { MAP_VIEWPORT_KEY } from './_components/venue-map-internal';

const VenueMapInternal = dynamic(
  () => import('./_components/venue-map-internal'),
  {
    ssr: false,
    loading: () => (
      <div className="flex h-full w-full items-center justify-center bg-[#1A1A1A]">
        <div className="border-primary h-8 w-8 animate-spin rounded-full border-b-2"></div>
      </div>
    ),
  },
);

export default function VenueMapPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const t = useTranslations('screens.venueMap');
  const { safeBack } = useSafeBack();
  const { selectedCity, hasHydrated } = useCitySelection();

  const [venues, setVenues] = useState<VenueMapItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const lastBoundsRef = useRef<string | null>(null);
  const currentBoundsRef = useRef<{
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  } | null>(null);

  const { searchQuery, selectedTagIds } = useVenueFiltersStore();

  // Fresh open from venues list → clear saved viewport
  // Back from venue details → restore saved viewport
  const isFresh = searchParams.has('fresh');

  useEffect(() => {
    if (isFresh) {
      window.history.replaceState(null, '', window.location.pathname);
    }
  }, [isFresh]);

  const [initialViewport] = useState(() => {
    if (isFresh) {
      sessionStorage.removeItem(MAP_VIEWPORT_KEY);
      return null;
    }
    try {
      const raw = sessionStorage.getItem(MAP_VIEWPORT_KEY);
      if (raw) {
        const parsed = JSON.parse(raw);
        if (parsed.lat && parsed.lng && parsed.zoom) return parsed;
      }
    } catch {}
    return null;
  });

  const mapCenter = useMemo(
    () => ({
      lat:
        initialViewport?.lat ??
        selectedCity?.latitude ??
        BELGRADE_COORDS.latitude,
      lng:
        initialViewport?.lng ??
        selectedCity?.longitude ??
        BELGRADE_COORDS.longitude,
    }),
    [
      initialViewport?.lat,
      initialViewport?.lng,
      selectedCity?.latitude,
      selectedCity?.longitude,
    ],
  );

  const initialZoom = initialViewport?.zoom ?? 12;

  const fetchMapVenues = useCallback(
    async (bounds: {
      minLat: number;
      maxLat: number;
      minLng: number;
      maxLng: number;
    }) => {
      const filterKey = `${searchQuery}|${selectedTagIds.slice().sort().join(',')}`;
      const boundsKey = `${bounds.minLat.toFixed(5)},${bounds.maxLat.toFixed(5)},${bounds.minLng.toFixed(5)},${bounds.maxLng.toFixed(5)}|${filterKey}`;
      if (lastBoundsRef.current === boundsKey) return;
      lastBoundsRef.current = boundsKey;

      setIsLoading(true);
      try {
        const params: Record<
          string,
          | string
          | number
          | boolean
          | (string | number | boolean)[]
          | undefined
          | null
        > = { ...bounds };
        if (selectedTagIds?.length) params['Filter.TagIds'] = selectedTagIds;
        if (searchQuery) params['Filter.CompanyName'] = searchQuery;

        const response = await apiClient.get(
          API_ENDPOINTS.VENUES.MAP,
          paramsForReq(params),
        );
        setVenues(response.data.result);
      } finally {
        setIsLoading(false);
      }
    },
    [searchQuery, selectedTagIds],
  );

  const handleBoundsChange = useCallback(
    (bounds: {
      minLat: number;
      maxLat: number;
      minLng: number;
      maxLng: number;
    }) => {
      currentBoundsRef.current = bounds;
      fetchMapVenues(bounds);
    },
    [fetchMapVenues],
  );

  const fetchMapVenuesRef = useRef(fetchMapVenues);
  fetchMapVenuesRef.current = fetchMapVenues;

  // Re-fetch when filters change, using last known bounds
  useEffect(() => {
    if (currentBoundsRef.current) {
      lastBoundsRef.current = null;
      fetchMapVenuesRef.current(currentBoundsRef.current);
    }
  }, [searchQuery, selectedTagIds]);

  const handleVenueDetails = useCallback(
    (id: string) => {
      router.push(PAGE_ROUTES.VENUE_DETAIL(id));
    },
    [router],
  );

  if (!hasHydrated) {
    return (
      <div className={styles.wrapper}>
        <Header />
        <div className="flex flex-1 items-center justify-center">
          <Spinner />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.wrapper}>
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => safeBack(PAGE_ROUTES.VENUES)}
            priority
          />
        }
        title={t('title')}
      />

      <VenueFilters />

      <div className={styles.mapContainer}>
        <VenueMapInternal
          center={mapCenter}
          venues={venues}
          onBoundsChange={handleBoundsChange}
          onVenueDetails={handleVenueDetails}
          detailsLabel={t('details')}
          initialZoom={initialZoom}
        />
        {isLoading && (
          <div className={styles.loadingIndicator}>
            <div className={styles.smallSpinner} />
          </div>
        )}
      </div>
    </div>
  );
}
