'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import Image from 'next/image';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Spinner } from '@/components/ui/spinner';
import { ListItem } from '@/components/ui/list-item';
import { useInView } from 'react-intersection-observer';
import { useViewportItemCount } from '@/hooks/use-viewport-item-count';
import { useVenuesStore, VenueListItemDto } from './use-venues-store';
import { toast } from 'react-toastify';
import { checkPermissionStatus } from '@/lib/permissions';
import { PAGE_ROUTES } from '@/lib/routes';
import { useSettingsStore, DistanceUnits } from '@/app/b2c/[locale]/settings/use-settings-store';
import { metersToKilometers, metersToMiles } from '@/lib/distance-utils';
import styles from './venues.module.css';

export default function VenuesScreen() {
  const router = useRouter();
  const t = useTranslations('screens.venues');
  const tCommon = useTranslations('common');
  const [selectedVenue, setSelectedVenue] = useState<VenueListItemDto | null>(
    null,
  );
  const [isLocationDenied, setIsLocationDenied] = useState(false);
  const distanceUnits = useSettingsStore((state) => state.distanceUnits);

  const { ref: pageBottomRef, inView } = useInView({
    threshold: 0,
    rootMargin: '200px',
  });

  const limit = useViewportItemCount({
    itemHeight: 88, // Based on .venueItem padding and content
    reservedHeight: 60,
    buffer: 4,
  });

  const { venues, isLoading, hasMore, fetchVenues, reset } = useVenuesStore();

  useEffect(() => {
    return () => {
      reset();
    };
  }, [reset]);

  const loadData = async (offset: number) => {
    try {
      const position = await new Promise<GeolocationPosition>(
        (resolve, reject) => {
          navigator.geolocation.getCurrentPosition(resolve, reject);
        },
      );
      const { latitude, longitude } = position.coords;
      await fetchVenues(latitude, longitude, offset, limit);
    } catch (err: any) {
      if (err.code === 1) {
        // PERMISSION_DENIED
        setIsLocationDenied(true);
      } else {
        toast.error(t('toast.loadFailed'));
      }
    }
  };

  useEffect(() => {
    const init = async () => {
      if (limit === 0) return;
      const { locationAllowed } = await checkPermissionStatus();
      if (!locationAllowed) {
        setIsLocationDenied(true);
      } else {
        setIsLocationDenied(false);
        // Only load if we don't have venues yet or if it was previously denied
        if (venues.length === 0 && !isLoading) {
          await loadData(0);
        }
      }
    };
    init();

    window.addEventListener('focus', init);
    return () => window.removeEventListener('focus', init);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [limit, venues.length]); // Fetch when limit is calculated or returned from background

  useEffect(() => {
    if (inView && hasMore && !isLoading && venues.length > 0) {
      loadData(venues.length);
    }
  }, [inView, hasMore, isLoading, venues.length]);

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

  return (
    <div className="flex flex-1 flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={20}
            height={26}
            onClick={() => router.replace(PAGE_ROUTES.FEED)}
            priority
          />
        }
        title={t('title')}
      />

      <div className={styles.content}>
        {isLoading && venues.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : isLocationDenied ? (
          <div className={styles.emptyState}>
            <Image
              src="/assets/venue/geolocation-disabled-placeholder.webp"
              alt="location required"
              width={300}
              height={300}
            />
            <span className={styles.emptyText}>{t('locationRequired')}</span>
          </div>
        ) : venues.length === 0 && !isLoading ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyText}>{t('noVenues')}</span>
          </div>
        ) : (
          <div className={styles.venueList}>
            {venues.map((venue) => (
              <ListItem
                key={venue.id}
                className="cursor-pointer"
                onClick={() => setSelectedVenue(venue)}
                avatarUrl={venue.companyAvatarImageUrl || '/assets/common/venues-icon.svg'}
                avatarAlt={venue.companyName}
                title={venue.companyName}
                subtitle={
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
    </div>
  );
}
