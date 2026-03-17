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
import { toast } from 'react-toastify';
import { checkPermissionStatus } from '@/lib/permissions';
import { PAGE_ROUTES } from '@/lib/routes';
import { useOffersStore, AvailableDiscountDto } from './use-offers-store';
import styles from './page.module.css';

export default function OffersAndDealsScreen() {
  const router = useRouter();
  const t = useTranslations('screens.offersAndDeals');
  const [selectedOffer, setSelectedOffer] =
    useState<AvailableDiscountDto | null>(null);
  const [isLocationDenied, setIsLocationDenied] = useState(false);

  const { ref: pageBottomRef, inView } = useInView({
    threshold: 0,
    rootMargin: '200px',
  });

  const limit = useViewportItemCount({
    itemHeight: 88, // Based on list-item padding and content height
    reservedHeight: 60,
    buffer: 4,
  });

  const { offers, isLoading, hasMore, fetchOffers, reset } = useOffersStore();

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
      await fetchOffers(latitude, longitude, offset, limit);
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
        // Only load if we don't have offers yet or if it was previously denied
        if (offers.length === 0 && !isLoading) {
          await loadData(0);
        }
      }
    };
    init();

    window.addEventListener('focus', init);
    return () => window.removeEventListener('focus', init);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [limit, offers.length]); // Fetch when limit is calculated

  useEffect(() => {
    if (inView && hasMore && !isLoading && offers.length > 0) {
      loadData(offers.length);
    }
  }, [inView, hasMore, isLoading, offers.length]);

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

  return (
    <div className="flex flex-1 flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={20}
            height={26}
            onClick={() => router.replace(PAGE_ROUTES.PROFILE)}
            priority
          />
        }
        title={t('title')}
      />

      <div className={styles.content}>
        {isLoading && offers.length === 0 ? (
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
        ) : offers.length === 0 && !isLoading ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyText}>{t('noOffers')}</span>
          </div>
        ) : (
          <div className={styles.offerList}>
            {offers.map((offer) => (
              <ListItem
                key={offer.venueId}
                className="cursor-pointer"
                onClick={() => setSelectedOffer(offer)}
                avatarUrl={
                  offer.companyAvatarImageUrl ||
                  '/assets/common/venues-icon.svg'
                }
                avatarAlt={offer.companyName}
                title={offer.companyName}
                subtitle={offer.venueAddress}
                rightEl={
                  <div className="flex items-center gap-2">
                    <span className="text-primary text-base font-medium">
                      {offer.percent}%
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
        )}
      </div>

      <ModalMenu
        isOpen={!!selectedOffer}
        onClose={() => setSelectedOffer(null)}
        options={selectedOffer ? getMenuOptions(selectedOffer) : []}
      />
    </div>
  );
}
