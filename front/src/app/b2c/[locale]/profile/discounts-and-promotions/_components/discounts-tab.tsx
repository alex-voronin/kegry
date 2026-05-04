import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { useRouter } from '@/i18n/routing';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Spinner } from '@/components/ui/spinner';
import { ListItem } from '@/components/ui/list-item';
import { useInView } from 'react-intersection-observer';
import { useViewportItemCount } from '@/hooks/use-viewport-item-count';
import { toast } from 'react-toastify';
import { PAGE_ROUTES } from '@/lib/routes';
import { getLowAccuracyLocation } from '@/lib/geo-utils';
import { City } from '@/lib/cities';
import {
  useDiscountsStore,
  AvailableDiscountDto,
} from '../use-discounts-store';
import styles from '../page.module.css';

interface DiscountsTabProps {
  selectedCity: City | null;
  isLocationDenied: boolean;
}

export function DiscountsTab({
  selectedCity,
  isLocationDenied,
}: DiscountsTabProps) {
  const router = useRouter();
  const t = useTranslations('screens.discountsAndPromotions');
  const [selectedOffer, setSelectedOffer] =
    useState<AvailableDiscountDto | null>(null);
  const [isGettingLocation, setIsGettingLocation] = useState(false);

  const { ref: pageBottomRef, inView } = useInView({
    threshold: 0,
    rootMargin: '200px',
  });

  const limit = useViewportItemCount({
    itemHeight: 88,
    reservedHeight: 110, // Header + tab bar
    buffer: 4,
  });

  const { offers, isLoading, hasMore, fetchOffers, reset } =
    useDiscountsStore();

  const loadData = async (offset: number, cityOverride?: City | null) => {
    setIsGettingLocation(true);
    const city = cityOverride !== undefined ? cityOverride : selectedCity;

    try {
      let lat, lon;
      if (city) {
        lat = city.latitude;
        lon = city.longitude;
      } else {
        const coords = await getLowAccuracyLocation();
        lat = coords.latitude;
        lon = coords.longitude;
      }
      await fetchOffers(lat, lon, offset, limit);
    } catch (err: any) {
      if (err.code !== 1) {
        toast.error(t('toast.loadFailed'));
      }
    } finally {
      setIsGettingLocation(false);
    }
  };

  useEffect(() => {
    reset();
  }, [selectedCity, reset]);

  useEffect(() => {
    const init = async () => {
      if (limit === 0) return;

      if (offers.length === 0 && !isLoading) {
        await loadData(0, selectedCity);
      }
    };
    init();

    window.addEventListener('focus', init);
    return () => window.removeEventListener('focus', init);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [limit, offers.length, selectedCity]);

  useEffect(() => {
    if (inView && hasMore && !isLoading && offers.length > 0) {
      loadData(offers.length, selectedCity);
    }
  }, [inView, hasMore, isLoading, offers.length, selectedCity]);

  const getMenuOptions = (offer: AvailableDiscountDto) => [
    {
      icon: '/assets/venue/venue-icon.svg',
      label: t('menu.viewVenue'),
      onClick: () => {
        router.push(PAGE_ROUTES.VENUE_DETAIL(offer.venueId));
        setSelectedOffer(null);
      },
    },
    ...(offer.googleMapsUrl
      ? [
          {
            icon: '/assets/venue/google-maps-icon.svg',
            label: t('menu.openInMaps'),
            onClick: () => {
              window.open(offer.googleMapsUrl as string, '_blank');
              setSelectedOffer(null);
            },
          },
        ]
      : []),
  ];

  if ((isLoading || isGettingLocation) && offers.length === 0) {
    return (
      <div className={styles.emptyState}>
        <Spinner />
      </div>
    );
  }

  if (isLocationDenied && !selectedCity) {
    return (
      <div className={styles.emptyState}>
        <Image
          src="/assets/venue/geolocation-disabled-placeholder.webp"
          alt="location required"
          width={300}
          height={300}
        />
        <span className={styles.emptyText}>{t('locationRequired')}</span>
      </div>
    );
  }

  if (offers.length === 0 && !isLoading) {
    return (
      <div className={styles.emptyState}>
        <span className={styles.emptyText}>{t('noOffers')}</span>
      </div>
    );
  }

  return (
    <>
      <div className={styles.offerList}>
        {offers.map((offer) => (
          <ListItem
            key={offer.venueId}
            className="cursor-pointer"
            onClick={() => setSelectedOffer(offer)}
            avatarUrl={
              offer.companyAvatarImageUrl || '/assets/common/venues-icon.svg'
            }
            avatarAlt={offer.companyName}
            title={offer.companyName}
            subtitle={offer.venueAddress}
            rightEl={
              <div className="flex items-center gap-2">
                <span className="text-primary text-base font-semibold">
                  {offer.percent === null
                    ? t('customDiscount')
                    : `${offer.percent}%`}
                </span>
                <Icon
                  src="/assets/common/more-icon.svg"
                  alt="more"
                  width={24}
                  height={24}
                  priority
                  stopPropagation
                  onClick={() => setSelectedOffer(offer)}
                />
              </div>
            }
          />
        ))}
        {hasMore && (
          <div ref={pageBottomRef} className="flex justify-center py-4">
            <Spinner />
          </div>
        )}
      </div>

      <ModalMenu
        isOpen={!!selectedOffer}
        onClose={() => setSelectedOffer(null)}
        options={selectedOffer ? getMenuOptions(selectedOffer) : []}
      />
    </>
  );
}
