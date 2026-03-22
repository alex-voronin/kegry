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
import { checkPermissionStatus } from '@/lib/permissions';
import { PAGE_ROUTES } from '@/lib/routes';
import { PromoIcon } from '@/lib/promo-types';
import {
  usePromotionsStore,
  AvailablePromotionDto,
} from '../use-promotions-store';
import styles from '../page.module.css';

export function PromotionsTab() {
  const router = useRouter();
  const t = useTranslations('screens.offersAndDeals');
  const [selectedPromotion, setSelectedPromotion] =
    useState<AvailablePromotionDto | null>(null);
  const [isLocationDenied, setIsLocationDenied] = useState(false);

  const { ref: pageBottomRef, inView } = useInView({
    threshold: 0,
    rootMargin: '200px',
  });

  const limit = useViewportItemCount({
    itemHeight: 88,
    reservedHeight: 110, // Header + tab bar
    buffer: 4,
  });

  const { promotions, isLoading, hasMore, fetchPromotions } =
    usePromotionsStore();

  const loadData = async (offset: number) => {
    try {
      const position = await new Promise<GeolocationPosition>(
        (resolve, reject) => {
          navigator.geolocation.getCurrentPosition(resolve, reject);
        },
      );
      const { latitude, longitude } = position.coords;
      await fetchPromotions(latitude, longitude, offset, limit);
    } catch (err: any) {
      if (err.code === 1) {
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
        if (promotions.length === 0 && !isLoading) {
          await loadData(0);
        }
      }
    };
    init();

    window.addEventListener('focus', init);
    return () => window.removeEventListener('focus', init);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [limit, promotions.length]);

  useEffect(() => {
    if (inView && hasMore && !isLoading && promotions.length > 0) {
      loadData(promotions.length);
    }
  }, [inView, hasMore, isLoading, promotions.length]);

  const getMenuOptions = (promotion: AvailablePromotionDto) => [
    {
      icon: '/assets/venue/venue-icon.svg',
      label: t('menu.viewVenue'),
      onClick: () => {
        router.push(PAGE_ROUTES.VENUE_DETAIL(promotion.venueId));
        setSelectedPromotion(null);
      },
    },
    ...(promotion.googleMapsUrl
      ? [
          {
            icon: '/assets/venue/google-maps-icon.svg',
            label: t('menu.openInMaps'),
            onClick: () => {
              window.open(promotion.googleMapsUrl as string, '_blank');
              setSelectedPromotion(null);
            },
          },
        ]
      : []),
  ];

  if (isLoading && promotions.length === 0) {
    return (
      <div className={styles.emptyState}>
        <Spinner />
      </div>
    );
  }

  if (isLocationDenied) {
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

  if (promotions.length === 0 && !isLoading) {
    return (
      <div className={styles.emptyState}>
        <span className={styles.emptyText}>{t('noPromotions')}</span>
      </div>
    );
  }

  return (
    <>
      <div className={styles.offerList}>
        {promotions.map((promo) => (
          <ListItem
            key={promo.venueId}
            className="cursor-pointer"
            onClick={() => setSelectedPromotion(promo)}
            avatarUrl={
              promo.companyAvatarImageUrl ||
              '/assets/common/venues-icon.svg'
            }
            avatarAlt={promo.companyName}
            title={promo.companyName}
            subtitle={promo.venueAddress}
            rightEl={
              <div className="flex items-center gap-2">
                <div className="text-primary flex items-center gap-1">
                  {promo.promos.length % 2 !== 0 && (
                    <PromoIcon
                      type={promo.promos[0].type}
                      size={16}
                    />
                  )}
                  {promo.promos.length > 1 && (
                    <div className="grid grid-flow-col grid-rows-2 gap-1">
                      {promo.promos
                        .slice(promo.promos.length % 2)
                        .map((p, idx) => (
                          <PromoIcon key={idx} type={p.type} size={16} />
                        ))}
                    </div>
                  )}
                </div>
                <Icon
                  src="/assets/common/more-icon.svg"
                  alt="more"
                  width={24}
                  height={24}
                  priority
                  stopPropagation
                  onClick={() => setSelectedPromotion(promo)}
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
        isOpen={!!selectedPromotion}
        onClose={() => setSelectedPromotion(null)}
        options={
          selectedPromotion ? getMenuOptions(selectedPromotion) : []
        }
      />
    </>
  );
}
