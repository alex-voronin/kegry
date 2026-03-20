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
import { ButtonVariant } from '@/components/ui/button';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useVenueStore } from './use-venue-store';
import { toast } from 'react-toastify';
import { cn } from '@/lib/utils';
import styles from './venue-detail.module.css';
import { InstagramIcon } from './_components/instagram-icon';
import {
  Plus,
  MoreHorizontal,
  BadgeCheck,
  BadgeAlert,
  Settings,
} from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';

export default function VenueDetailPage() {
  const router = useRouter();
  const params = useParams();
  const venueId = params.id as string;
  const t = useTranslations('screens.venueDetail');
  const tCommon = useTranslations('common');

  const { currentVenue, isLoading, fetchVenue, deleteVenue, reset } =
    useVenueStore();
  const [isInitializing, setIsInitializing] = useState(true);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [selectedPromoId, setSelectedPromoId] = useState<string | null>(null);
  const [isPromoMenuOpen, setIsPromoMenuOpen] = useState(false);
  const [selectedDiscountId, setSelectedDiscountId] = useState<string | null>(
    null,
  );
  const [isDiscountMenuOpen, setIsDiscountMenuOpen] = useState(false);
  const [isPromoDeleteModalOpen, setIsPromoDeleteModalOpen] = useState(false);
  const [isDiscountDeleteModalOpen, setIsDiscountDeleteModalOpen] =
    useState(false);

  const { safeBack } = useSafeBack();

  useEffect(() => {
    const loadVenue = async () => {
      setIsInitializing(true);
      try {
        await fetchVenue(venueId);
      } catch (error: any) {
        toast.error(error.message || t('notFound'));
      } finally {
        setIsInitializing(false);
      }
    };

    if (venueId) {
      loadVenue();
    }
  }, [venueId, fetchVenue, t]);

  const handleBack = () => {
    safeBack(PAGE_ROUTES.B2B.COMPANIES);
  };

  const handleEditVenue = () => {
    setIsMenuOpen(false);
    router.push(PAGE_ROUTES.B2B.VENUES_EDIT(venueId));
  };

  const handleDeleteVenue = () => {
    setIsMenuOpen(false);
    setIsDeleteModalOpen(true);
  };

  const handleDeleteConfirm = async () => {
    setIsDeleting(true);
    try {
      await deleteVenue(venueId);
      router.back();
    } catch (error: any) {
      toast.error(error.message || t('toast.deleteFailed'));
    } finally {
      setIsDeleting(false);
      setIsDeleteModalOpen(false);
    }
  };

  const handleAddPromo = () => {
    router.push(PAGE_ROUTES.B2B.VENUES_DETAILS_PROMOTION_CREATE(venueId));
  };

  const handleAddDiscount = () => {
    router.push(PAGE_ROUTES.B2B.VENUES_DETAILS_DISCOUNT_CREATE(venueId));
  };

  const handlePromoMenuOpen = (promoId: string) => {
    setSelectedPromoId(promoId);
    setIsPromoMenuOpen(true);
  };

  const handleDiscountMenuOpen = (discountId: string) => {
    setSelectedDiscountId(discountId);
    setIsDiscountMenuOpen(true);
  };

  const { deletePromotion, deleteDiscount } = useVenueStore();

  const handleEditPromo = () => {
    if (!selectedPromoId) return;
    setIsPromoMenuOpen(false);
    router.push(
      PAGE_ROUTES.B2B.VENUES_DETAILS_PROMOTION_EDIT(venueId, selectedPromoId),
    );
  };

  const handleDeletePromo = () => {
    setIsPromoMenuOpen(false);
    setIsPromoDeleteModalOpen(true);
  };

  const handlePromoDeleteConfirm = async () => {
    if (!selectedPromoId) return;
    setIsPromoDeleteModalOpen(false);
    try {
      await deletePromotion(venueId, selectedPromoId);
    } catch (error: any) {
      toast.error(error.message || t('toast.deleteFailed'));
    }
  };

  const handleEditDiscount = () => {
    if (!selectedDiscountId) return;
    setIsDiscountMenuOpen(false);
    router.push(
      PAGE_ROUTES.B2B.VENUES_DETAILS_DISCOUNT_EDIT(venueId, selectedDiscountId),
    );
  };

  const handleDeleteDiscount = () => {
    setIsDiscountMenuOpen(false);
    setIsDiscountDeleteModalOpen(true);
  };

  const handleDiscountDeleteConfirm = async () => {
    if (!selectedDiscountId) return;
    setIsDiscountDeleteModalOpen(false);
    try {
      await deleteDiscount(venueId, selectedDiscountId);
    } catch (error: any) {
      toast.error(error.message || t('toast.deleteFailed'));
    }
  };

  const discountMenuOptions = [
    {
      icon: '/assets/b2b/edit-icon.svg',
      label: t('menu.editDiscount'),
      onClick: handleEditDiscount,
    },
    {
      icon: '/assets/b2b/delete-icon.svg',
      label: t('menu.deleteDiscount'),
      onClick: handleDeleteDiscount,
      isDestructive: true,
    },
  ];

  const promoMenuOptions = [
    {
      icon: '/assets/b2b/edit-icon.svg',
      label: t('menu.editPromotion'),
      onClick: handleEditPromo,
    },
    {
      icon: '/assets/b2b/delete-icon.svg',
      label: t('menu.deletePromotion'),
      onClick: handleDeletePromo,
      isDestructive: true,
    },
  ];

  const handleAdvanced = () => {
    setIsMenuOpen(false);
    router.push(PAGE_ROUTES.B2B.VENUES_DETAILS_ADVANCED(venueId));
  };

  const menuOptions = [
    {
      icon: '/assets/b2b/edit-icon.svg',
      label: t('menu.editVenue'),
      onClick: handleEditVenue,
    },
    {
      icon: <Settings size={24} className="text-primary" />,
      label: t('menu.advanced'),
      onClick: handleAdvanced,
    },
    {
      icon: '/assets/b2b/delete-icon.svg',
      label: t('menu.deleteVenue'),
      onClick: handleDeleteVenue,
      isDestructive: true,
    },
  ];

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
            <div
              className={cn(
                styles.statusBadge,
                currentVenue.isVerified
                  ? styles.statusVerified
                  : styles.statusNonVerified,
              )}
            >
              {currentVenue.isVerified ? (
                <BadgeCheck size={14} />
              ) : (
                <BadgeAlert size={14} />
              )}
              <span>
                {currentVenue.isVerified ? t('verified') : t('nonVerified')}
              </span>
            </div>
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
                  <div
                    key={promo.id || idx}
                    className={`${styles.promoItem} ${styles.promoItemActionable}`}
                    onClick={() => handlePromoMenuOpen(promo.id)}
                  >
                    <div className={styles.promoItemHeader}>
                      <div className={styles.promoBody}>{promo.body}</div>
                      <div className="p-1">
                        <MoreHorizontal size={20} className="text-secondary" />
                      </div>
                    </div>
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
                  <div
                    key={discount.id}
                    className={`${styles.discountItem} ${styles.promoItemActionable}`}
                    onClick={() => handleDiscountMenuOpen(discount.id)}
                  >
                    <div className={styles.discountHeader}>
                      <div className="flex items-center gap-2">
                        <span className={styles.discountPercent}>
                          {discount.percent}%
                        </span>
                        {discount.loyaltyLevelId && (
                          <span className="text-secondary text-sm">
                            Level {discount.loyaltyLevelId}
                          </span>
                        )}
                      </div>
                      <div className="p-1">
                        <MoreHorizontal size={20} className="text-secondary" />
                      </div>
                    </div>
                    {discount.days && discount.days.length > 0 && (
                      <div className={styles.discountDays}>
                        {discount.days
                          .map((day) => tCommon(`days.${day}`))
                          .join(', ')}
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

      <ModalMenu
        isOpen={isPromoMenuOpen}
        onClose={() => setIsPromoMenuOpen(false)}
        options={promoMenuOptions}
      />

      <ModalMenu
        isOpen={isDiscountMenuOpen}
        onClose={() => setIsDiscountMenuOpen(false)}
        options={discountMenuOptions}
      />

      <ModalDialog
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        message={t('deleteDialog.title')}
        primaryButtonText={t('deleteDialog.buttonNo')}
        secondaryButtonText={t('deleteDialog.buttonYes')}
        secondaryButtonVariant={ButtonVariant.Destructive}
        onPrimaryAction={() => setIsDeleteModalOpen(false)}
        onSecondaryAction={handleDeleteConfirm}
      />

      <ModalDialog
        isOpen={isPromoDeleteModalOpen}
        onClose={() => setIsPromoDeleteModalOpen(false)}
        message={t('deletePromoDialog.title')}
        primaryButtonText={t('deleteDialog.buttonNo')}
        secondaryButtonText={t('deleteDialog.buttonYes')}
        secondaryButtonVariant={ButtonVariant.Destructive}
        onPrimaryAction={() => setIsPromoDeleteModalOpen(false)}
        onSecondaryAction={handlePromoDeleteConfirm}
      />

      <ModalDialog
        isOpen={isDiscountDeleteModalOpen}
        onClose={() => setIsDiscountDeleteModalOpen(false)}
        message={t('deleteDiscountDialog.title')}
        primaryButtonText={t('deleteDialog.buttonNo')}
        secondaryButtonText={t('deleteDialog.buttonYes')}
        secondaryButtonVariant={ButtonVariant.Destructive}
        onPrimaryAction={() => setIsDiscountDeleteModalOpen(false)}
        onSecondaryAction={handleDiscountDeleteConfirm}
      />
    </div>
  );
}
