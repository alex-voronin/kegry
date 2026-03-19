'use client';

import { useEffect, useState } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { ModalMenu } from '@/components/ui/modal-menu';
import { useVenueStore } from './use-venue-store';
import { toast } from 'react-toastify';
import styles from './venue-detail.module.css';
import { InstagramIcon } from './_components/instagram-icon';
import { Plus } from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';

export default function VenueDetailPage() {
  const router = useRouter();
  const params = useParams();
  const venueId = params.id as string;
  const t = useTranslations('screens.venueDetail');

  const { currentVenue, isLoading, fetchVenue, reset } = useVenueStore();
  const [isInitializing, setIsInitializing] = useState(true);
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  useEffect(() => {
    const loadVenue = async () => {
      setIsInitializing(true);
      try {
        await fetchVenue(venueId);
      } catch (error) {
        toast.error(t('notFound'));
      } finally {
        setIsInitializing(false);
      }
    };

    if (venueId) {
      loadVenue();
    }

    return () => {
      reset();
    };
  }, [venueId, fetchVenue, reset, t]);

  const handleBack = () => {
    router.back();
  };

  const handleAddPromo = () => {
    router.push(PAGE_ROUTES.B2B.VENUES_DETAILS_PROMOTION_CREATE(venueId));
  };

  const handleAddDiscount = () => {
    // Placeholder for adding discount
    console.log('Add discount for venue:', venueId);
  };

  if (isInitializing || isLoading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner />
      </div>
    );
  }

  if (!currentVenue) {
    return (
      <div className={styles.wrapper}>
        <Header
          title={t('title')}
          left={
            <Icon
              src="/assets/b2b/disclosure-left-icon.svg"
              alt="back"
              width={14}
              height={27}
              onClick={handleBack}
            />
          }
        />
        <div className={styles.emptyState}>
          <span>{t('notFound')}</span>
        </div>
      </div>
    );
  }

  const menuOptions = [
    {
      label: t('menu.openInMaps'),
      icon: '/assets/b2b/google-maps-icon.svg',
      onClick: () => {
        if (currentVenue.googleMapsUrl) {
          window.open(currentVenue.googleMapsUrl, '_blank');
        }
      },
    },
    ...(currentVenue.instagramUrl
      ? [
          {
            label: t('menu.openInstagram'),
            icon: <InstagramIcon />,
            onClick: () =>
              window.open(currentVenue.instagramUrl as string, '_blank'),
          },
        ]
      : []),
    ...(currentVenue.websiteUrl
      ? [
          {
            label: t('menu.openWebsite'),
            icon: '/assets/b2b/website-icon.svg',
            onClick: () =>
              window.open(currentVenue.websiteUrl as string, '_blank'),
          },
        ]
      : []),
  ];

  return (
    <div className={styles.wrapper}>
      <div className={styles.coverContainer}>
        <Header
          className={styles.header}
          left={
            <Icon
              src="/assets/b2b/disclosure-left-icon.svg"
              alt="back"
              width={14}
              height={27}
              onClick={handleBack}
            />
          }
          title={t('title')}
          right={
            <Icon
              src="/assets/b2b/more-icon.svg"
              alt="more"
              width={24}
              height={24}
              onClick={() => setIsMenuOpen(true)}
            />
          }
        />
        {currentVenue.coverImageUrl ? (
          <>
            <Image
              src={currentVenue.coverImageUrl}
              alt="Venue Cover"
              fill
              className={styles.coverImage}
              priority
            />
            <div className={styles.coverOverlay} />
          </>
        ) : (
          <div className="bg-surface-dark h-full w-full" />
        )}
      </div>

      <div className={styles.mainContent}>
        <div className={styles.content}>
          <div className={styles.section}>
            <FormSectionTitle>{t('address')}</FormSectionTitle>
            <div
              className={`${styles.field} ${
                currentVenue.googleMapsUrl ? styles.actionField : ''
              }`}
              onClick={() => {
                if (currentVenue.googleMapsUrl) {
                  window.open(currentVenue.googleMapsUrl, '_blank');
                }
              }}
            >
              <Image
                src="/assets/b2b/google-maps-icon.svg"
                alt="Maps"
                width={24}
                height={24}
              />
              <span className={styles.fieldText}>{currentVenue.address}</span>
            </div>
          </div>

          {(currentVenue.phoneNumber ||
            currentVenue.websiteUrl ||
            currentVenue.instagramUrl) && (
            <div className={styles.section}>
              <FormSectionTitle>{t('contacts')}</FormSectionTitle>

              {currentVenue.phoneNumber && (
                <div className={styles.field}>
                  <Image
                    src="/assets/b2b/phone-icon.svg"
                    alt="Phone"
                    width={24}
                    height={24}
                  />
                  <span>{currentVenue.phoneNumber}</span>
                </div>
              )}

              {currentVenue.websiteUrl && (
                <div
                  className={`${styles.field} ${styles.actionField}`}
                  onClick={() =>
                    window.open(currentVenue.websiteUrl as string, '_blank')
                  }
                >
                  <Image
                    src="/assets/b2b/website-icon.svg"
                    alt="Website"
                    width={24}
                    height={24}
                  />
                  <span className={styles.fieldText}>
                    {currentVenue.websiteUrl}
                  </span>
                </div>
              )}

              {currentVenue.instagramUrl && (
                <div
                  className={`${styles.field} ${styles.actionField}`}
                  onClick={() =>
                    window.open(currentVenue.instagramUrl as string, '_blank')
                  }
                >
                  <InstagramIcon />
                  <span className={styles.fieldText}>
                    {currentVenue.instagramUrl
                      .split('/')
                      .filter(Boolean)
                      .at(-1)}
                  </span>
                </div>
              )}
            </div>
          )}

          {currentVenue.workingHours && (
            <div className={styles.section}>
              <FormSectionTitle>{t('workingHours')}</FormSectionTitle>
              <div className={styles.field}>
                <div className={styles.workingHoursText}>
                  {currentVenue.workingHours}
                </div>
              </div>
            </div>
          )}

          {currentVenue.promos && currentVenue.promos.length > 0 && (
            <div className={styles.section}>
              <div className={styles.listHeader}>
                <h2 className={styles.listTitle}>{t('promotions')}</h2>
                <div onClick={handleAddPromo} className="cursor-pointer">
                  <Plus size={24} className="text-primary" />
                </div>
              </div>
              <div className={styles.promoList}>
                {currentVenue.promos.map((promo, idx) => (
                  <div key={idx} className={styles.promoItem}>
                    {promo.body}
                  </div>
                ))}
              </div>
            </div>
          )}

          {currentVenue.discounts && currentVenue.discounts.length > 0 && (
            <div className={styles.section}>
              <div className={styles.listHeader}>
                <h2 className={styles.listTitle}>{t('discounts')}</h2>
                <div onClick={handleAddDiscount} className="cursor-pointer">
                  <Plus size={24} className="text-primary" />
                </div>
              </div>
              <div className={styles.discountList}>
                {currentVenue.discounts.map((discount) => (
                  <div key={discount.id} className={styles.discountItem}>
                    <div className={styles.discountHeader}>
                      <span className={styles.discountPercent}>
                        {discount.percent}%
                      </span>
                      {discount.loyaltyLevelId && (
                        <span className="text-secondary text-sm">
                          Level {discount.loyaltyLevelId}
                        </span>
                      )}
                    </div>
                    {discount.days && discount.days.length > 0 && (
                      <div className={styles.discountDays}>
                        {discount.days.map((day) => t(`days.${day}`)).join(', ')}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      <ModalMenu
        isOpen={isMenuOpen}
        onClose={() => setIsMenuOpen(false)}
        options={menuOptions}
      />
    </div>
  );
}
