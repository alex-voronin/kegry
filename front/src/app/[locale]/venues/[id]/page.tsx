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
import { useVenueStore } from '@/store/use-venue-store';
import { cn } from '@/lib/utils';
import { toast } from 'react-toastify';
import styles from './venue-detail.module.css';
import { InstagramIcon } from './_components/instagram-icon';

export default function VenueDetailScreen() {
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

  if (isInitializing || isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  if (!currentVenue) {
    return (
      <div className="flex h-screen flex-1 flex-col">
        <Header
          left={
            <Icon
              src="/assets/common/disclosure-left-icon.svg"
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

  const { company, venue, loyaltyLevel } = currentVenue;

  return (
    <div className={styles.wrapper}>
      {/* Cover Image */}
      <div className={styles.coverContainer}>
        <Header
          className={styles.header}
          left={
            <Icon
              src="/assets/common/disclosure-left-icon.svg"
              alt="back"
              width={20}
              height={26}
              onClick={handleBack}
            />
          }
          right={
            <Icon
              src="/assets/common/more-icon.svg"
              alt="more"
              width={24}
              height={24}
              onClick={() => setIsMenuOpen(true)}
            />
          }
        />
        {venue.coverImageUrl ? (
          <Image
            src={venue.coverImageUrl}
            alt="Venue Cover"
            fill
            className={styles.coverImage}
            priority
          />
        ) : (
          <div className="h-full w-full bg-slate-300" />
        )}
        <div className={styles.coverOverlay} />
      </div>

      {/* Main Content Area (Rounded Top) */}
      <div className={styles.mainContent}>
        {/* Overlapping Avatar */}
        <div className={styles.avatarSection}>
          <div className={styles.avatarContainer}>
            <Image
              src={company.avatarImageUrl || '/assets/common/venues-icon.svg'}
              alt={company.name}
              fill
              className={styles.avatarImage}
            />
          </div>
          <div className={styles.loyaltyLevel}>{loyaltyLevel}</div>
        </div>

        {/* Company Name & Instagram */}
        <div className={styles.companyHeader}>
          <div className={styles.companyNameWrapper}>
            <h1 className={styles.userName}>{company.name}</h1>
            {venue.instagramUrl && (
              <div
                className={styles.instagramWrapper}
                onClick={() =>
                  window.open(venue.instagramUrl as string, '_blank')
                }
              >
                <InstagramIcon />
              </div>
            )}
          </div>
        </div>

        <div className={styles.content}>
          <div className={styles.section}>
            <FormSectionTitle>{t('address')}</FormSectionTitle>
            <div
              className={cn(styles.field, styles.actionField)}
              onClick={() => window.open(venue.googleMapsUrl, '_blank')}
            >
              <div className={styles.actionLabel}>
                <Image
                  src="/assets/venue/google-maps-icon.svg"
                  alt="Maps"
                  width={24}
                  height={24}
                />
                <span className={styles.fieldText}>{venue.address}</span>
              </div>
            </div>
          </div>

          {(venue.phoneNumber || venue.websiteUrl) && (
            <div className={styles.section}>
              <FormSectionTitle>{t('contacts')}</FormSectionTitle>

              {/* Phone Number */}
              {venue.phoneNumber && (
                <div className={styles.field}>
                  <div className={styles.actionLabel}>
                    <Image
                      src="/assets/venue/phone-icon.svg"
                      alt="Phone"
                      width={24}
                      height={24}
                    />
                    <span>{venue.phoneNumber}</span>
                  </div>
                </div>
              )}

              {/* Website */}
              {venue.websiteUrl && (
                <div
                  className={cn(styles.field, styles.actionField)}
                  onClick={() =>
                    window.open(venue.websiteUrl as string, '_blank')
                  }
                >
                  <div className={styles.actionLabel}>
                    <Image
                      src="/assets/venue/website-icon.svg"
                      alt="Website"
                      width={24}
                      height={24}
                    />
                    <span className={styles.fieldText}>{venue.websiteUrl}</span>
                  </div>
                </div>
              )}
            </div>
          )}

          {venue.workingHours && (
            <div className={styles.section}>
              <FormSectionTitle>{t('workingHours')}</FormSectionTitle>
              <div className={styles.field}>
                <div className={styles.workingHoursText}>
                  {venue.workingHours}
                </div>
              </div>
            </div>
          )}

          {venue.promos && venue.promos.length > 0 && (
            <div className={styles.section}>
              <FormSectionTitle>{t('promotions')}</FormSectionTitle>
              <div className={styles.promoList}>
                {venue.promos.map((promo, idx) => (
                  <div key={idx} className={styles.promoItem}>
                    {promo.body}
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
        options={[
          {
            label: t('menu.openInMaps'),
            icon: '/assets/venue/google-maps-icon.svg',
            onClick: () => window.open(venue.googleMapsUrl, '_blank'),
          },
          ...(venue.instagramUrl
            ? [
                {
                  label: t('menu.openInstagram'),
                  icon: <InstagramIcon />,
                  onClick: () =>
                    window.open(venue.instagramUrl as string, '_blank'),
                },
              ]
            : []),
          ...(venue.websiteUrl
            ? [
                {
                  label: t('menu.openWebsite'),
                  icon: '/assets/venue/website-icon.svg',
                  onClick: () =>
                    window.open(venue.websiteUrl as string, '_blank'),
                },
              ]
            : []),
        ]}
      />
    </div>
  );
}
