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
import { ModalDialog } from '@/components/ui/modal-dialog';
import { useVenueStore } from './use-venue-store';
import { PAGE_ROUTES } from '@/lib/routes';
import { cn } from '@/lib/utils';
import { toast } from 'react-toastify';
import { HelpCircle, ChevronRight, ScanQrCode, Link } from 'lucide-react';
import styles from './venue-detail.module.css';
import { InstagramIcon } from './_components/instagram-icon';
import { PromoIcon } from '@/lib/promo-types';
import { useSafeBack } from '@/hooks/use-safe-back';
import { QrScannerPopup } from '../_components/qr-scanner-popup';
import { CheckInAnimation } from '../_components/check-in-animation';
import { useQrCheckIn } from '../use-qr-check-in';

function calculateProgressPercentage(
  count: number,
  lower: number,
  upper: number,
) {
  if (upper <= lower) return 100;
  return Math.max(0, Math.min(100, ((count - lower) / (upper - lower)) * 100));
}

export default function VenueDetailScreen() {
  const router = useRouter();
  const params = useParams();
  const venueId = params.id as string;
  const t = useTranslations('screens.venueDetail');

  const { currentVenue, isLoading, fetchVenue } = useVenueStore();
  const [isInitializing, setIsInitializing] = useState(
    () => currentVenue?.venue.id !== venueId,
  );
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isHelpDialogOpen, setIsHelpDialogOpen] = useState(false);
  const [checkInVenueId, setCheckInVenueId] = useState<string | null>(null);

  const {
    isScannerOpen,
    setIsScannerOpen,
    showCheckIn,
    setShowCheckIn,
    handleQrCode,
  } = useQrCheckIn({
    onSuccess: (id) => setCheckInVenueId(id),
  });

  const { safeBack } = useSafeBack();

  useEffect(() => {
    const loadVenue = async () => {
      // If we don't have the correct venue, show initializing loader
      if (useVenueStore.getState().currentVenue?.venue.id !== venueId) {
        setIsInitializing(true);
      }
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
  }, [venueId, fetchVenue, t]);

  const handleBack = () => {
    safeBack(PAGE_ROUTES.VENUES);
  };

  if ((isInitializing || isLoading) && currentVenue?.venue.id !== venueId) {
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
        <div className="flex flex-1 items-center justify-center">
          <Spinner />
        </div>
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
              width={14}
              height={27}
              onClick={handleBack}
            />
          }
          title={<h1 className={styles.companyName}>{company.name}</h1>}
          right={
            <div className="flex items-center gap-3">
              <Icon onClick={() => setIsScannerOpen(true)}>
                <ScanQrCode size={24} className="text-white" />
              </Icon>
              <Icon
                src="/assets/common/more-icon.svg"
                alt="more"
                width={24}
                height={24}
                onClick={() => setIsMenuOpen(true)}
              />
            </div>
          }
        />
        {venue.coverImageUrl ? (
          <>
            <Image
              src={venue.coverImageUrl}
              alt="Venue Cover"
              fill
              className={styles.coverImage}
              priority
            />
            <div className={styles.coverOverlay} />
          </>
        ) : (
          <div className="h-full w-full" />
        )}
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
        </div>

        <div className={styles.content}>
          {(loyaltyLevel.discount > 0 ||
            loyaltyLevel.userLoyaltyLevel > 0 ||
            loyaltyLevel.visitsUpperBound != null) && (
            <div className={styles.section}>
              <FormSectionTitle>{t('myDiscount')}</FormSectionTitle>
              <div
                className={cn(styles.field, styles.loyaltyCard)}
                onClick={() =>
                  router.push(PAGE_ROUTES.VENUE_DISCOUNTS(venueId))
                }
              >
                <div className={styles.loyaltyHeader}>
                  <div className={styles.loyaltyLevelTitle}>
                    <span>
                      {t('level')} {loyaltyLevel.userLoyaltyLevel}
                    </span>
                  </div>

                  <div className="flex items-center gap-2">
                    {loyaltyLevel.discount > 0 && (
                      <span className={styles.loyaltyDiscount}>
                        {t('discount', { percent: loyaltyLevel.discount })}
                      </span>
                    )}
                    <ChevronRight size={24} className="text-primary" />
                  </div>
                </div>
                {(loyaltyLevel.visitsUpperBound !== null ||
                  loyaltyLevel.userLoyaltyLevel > 0) && (
                  <div className={styles.progressContainer}>
                    <div className={styles.progressBarBg}>
                      <div
                        className={styles.progressBarFill}
                        style={{
                          width: `${
                            loyaltyLevel.visitsUpperBound === null
                              ? 100
                              : calculateProgressPercentage(
                                  loyaltyLevel.visitsCount,
                                  loyaltyLevel.visitsLowerBound,
                                  loyaltyLevel.visitsUpperBound,
                                )
                          }%`,
                        }}
                      />
                    </div>
                    <div className="flex items-center gap-1.5 self-end">
                      <div className={styles.progressText}>
                        {loyaltyLevel.visitsUpperBound === null
                          ? t('maxLoyaltyLevelReached')
                          : t('visits', {
                              current:
                                loyaltyLevel.visitsCount -
                                loyaltyLevel.visitsLowerBound,
                              total:
                                loyaltyLevel.visitsUpperBound -
                                loyaltyLevel.visitsLowerBound,
                            })}
                      </div>
                      <div
                        className="text-secondary cursor-pointer"
                        onClick={(e) => {
                          e.stopPropagation();
                          setIsHelpDialogOpen(true);
                        }}
                      >
                        <HelpCircle size={14} />
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

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

          {(venue.phoneNumber || venue.websiteUrl || venue.instagramUrl) && (
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

              {/* Instagram */}
              {venue.instagramUrl && (
                <div
                  className={cn(styles.field, styles.actionField)}
                  onClick={() =>
                    window.open(venue.instagramUrl as string, '_blank')
                  }
                >
                  <div className={styles.actionLabel}>
                    <InstagramIcon />
                    <span className={styles.fieldText}>
                      {venue.instagramUrl.split('/').at(-1)}
                    </span>
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
                    <div className="flex items-center gap-3">
                      <PromoIcon
                        type={promo.type}
                        className="text-primary shrink-0"
                        size={24}
                      />
                      <div className={styles.promoBody}>{promo.body}</div>
                    </div>
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
            label: t('menu.copyLink'),
            icon: <Link size={24} />,
            onClick: () => {
              navigator.clipboard.writeText(window.location.href);
              toast.success(t('toast.linkCopied'));
              setIsMenuOpen(false);
            },
          },
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

      <QrScannerPopup
        isOpen={isScannerOpen}
        onClose={() => setIsScannerOpen(false)}
        onScan={handleQrCode}
      />

      <ModalDialog
        isOpen={isHelpDialogOpen}
        onClose={() => setIsHelpDialogOpen(false)}
        message={t('helpDialog')}
        primaryButtonText={t('ok')}
        onPrimaryAction={() => setIsHelpDialogOpen(false)}
      />

      <CheckInAnimation
        visible={showCheckIn}
        onDone={() => {
          setShowCheckIn(false);
          if (checkInVenueId) {
            if (checkInVenueId === venueId) {
              fetchVenue(venueId);
            } else {
              router.push(PAGE_ROUTES.VENUE_DETAIL(checkInVenueId));
            }
            setCheckInVenueId(null);
          }
        }}
      />
    </div>
  );
}
