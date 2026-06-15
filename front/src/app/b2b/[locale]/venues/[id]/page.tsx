'use client';

import { useEffect, useState, useCallback } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { VenueTagAccordion } from '@/components/common/venue-tag-accordion';
import { TabControl } from '@/components/ui/tab-control';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { ButtonVariant } from '@/components/ui/button';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useVenueStore } from './use-venue-store';
import { toast } from 'react-toastify';
import { cn } from '@/lib/utils';
import styles from './venue-detail.module.css';
import { InstagramIcon } from './_components/instagram-icon';
import { QrPopup } from './_components/qr-popup';
import { buildQrUrl } from '@/lib/qr-utils';
import {
  Plus,
  MoreHorizontal,
  BadgeCheck,
  BadgeAlert,
  Settings,
  QrCode,
  SquarePen,
  Trash2,
  Calendar,
  Users,
  ChevronRight,
} from 'lucide-react';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { PromoIcon } from '@/lib/promo-types';
import { parseInstagramHandle } from '@/lib/url-utils';
import apiClient from '@/lib/api-client';
import type { B2BEventListItemDto } from '../../events/use-events-store';

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
  const [isQrPopupOpen, setIsQrPopupOpen] = useState(false);
  const [isVerificationDialogOpen, setIsVerificationDialogOpen] =
    useState(false);
  const [activeTab, setActiveTab] = useState('venue');
  const [venueEvents, setVenueEvents] = useState<B2BEventListItemDto[]>([]);
  const [isEventsLoading, setIsEventsLoading] = useState(false);
  const [selectedVenueEventId, setSelectedVenueEventId] = useState<string | null>(null);
  const [isVenueEventMenuOpen, setIsVenueEventMenuOpen] = useState(false);
  const [isVenueEventDeleteModalOpen, setIsVenueEventDeleteModalOpen] = useState(false);
  const [isVenueEventDeleting, setIsVenueEventDeleting] = useState(false);

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

  const loadVenueEvents = useCallback(async () => {
    if (!venueId) return;
    setIsEventsLoading(true);
    try {
      const response = await apiClient.get(API_ENDPOINTS.B2B.EVENTS.BASE, {
        params: { venueId, limit: 50 },
      });
      setVenueEvents(response.data.result.data ?? []);
    } catch {
      // silently fail
    } finally {
      setIsEventsLoading(false);
    }
  }, [venueId]);

  useEffect(() => {
    loadVenueEvents();
  }, [loadVenueEvents]);

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
      safeBack(PAGE_ROUTES.B2B.COMPANIES);
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
      icon: <SquarePen size={24} className="text-primary" />,
      label: t('menu.editDiscount'),
      onClick: handleEditDiscount,
    },
    {
      icon: <Trash2 size={24} className="text-error" />,
      label: t('menu.deleteDiscount'),
      onClick: handleDeleteDiscount,
      isDestructive: true,
    },
  ];

  const promoMenuOptions = [
    {
      icon: <SquarePen size={24} className="text-primary" />,
      label: t('menu.editPromotion'),
      onClick: handleEditPromo,
    },
    {
      icon: <Trash2 size={24} className="text-error" />,
      label: t('menu.deletePromotion'),
      onClick: handleDeletePromo,
      isDestructive: true,
    },
  ];

  const handleAdvanced = () => {
    setIsMenuOpen(false);
    router.push(PAGE_ROUTES.B2B.VENUES_DETAILS_ADVANCED(venueId));
  };

  const handleAddEvent = () => {
    router.push(PAGE_ROUTES.B2B.EVENTS_CREATE(venueId));
  };

  const handleVenueEventMenuOpen = (eventId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setSelectedVenueEventId(eventId);
    setIsVenueEventMenuOpen(true);
  };

  const handleEditVenueEvent = () => {
    if (!selectedVenueEventId) return;
    setIsVenueEventMenuOpen(false);
    router.push(PAGE_ROUTES.B2B.EVENTS_EDIT(selectedVenueEventId));
  };

  const handleDeleteVenueEvent = () => {
    setIsVenueEventMenuOpen(false);
    setIsVenueEventDeleteModalOpen(true);
  };

  const handleVenueEventDeleteConfirm = async () => {
    if (!selectedVenueEventId) return;
    setIsVenueEventDeleting(true);
    try {
      await apiClient.delete(API_ENDPOINTS.B2B.EVENTS.DETAILS(selectedVenueEventId));
      toast.success(t('toast.deleteSuccess'));
      loadVenueEvents();
    } catch (error: any) {
      toast.error(error.message || t('toast.deleteFailed'));
    } finally {
      setIsVenueEventDeleting(false);
      setIsVenueEventDeleteModalOpen(false);
      setSelectedVenueEventId(null);
    }
  };

  const menuOptions = [
    ...(currentVenue && !currentVenue.isVerified
      ? [
          {
            icon: <BadgeAlert size={24} className="text-primary" />,
            label: t('menu.verifyVenue'),
            onClick: () => {
              setIsMenuOpen(false);
              setIsVerificationDialogOpen(true);
            },
          },
        ]
      : []),
    {
      icon: <QrCode size={24} className="text-primary" />,
      label: t('menu.qrCode'),
      onClick: () => {
        setIsMenuOpen(false);
        setIsQrPopupOpen(true);
      },
    },
    {
      icon: <SquarePen size={24} className="text-primary" />,
      label: t('menu.editVenue'),
      onClick: handleEditVenue,
    },
    {
      icon: <Settings size={24} className="text-primary" />,
      label: t('menu.advanced'),
      onClick: handleAdvanced,
    },
    {
      icon: <Trash2 size={24} className="text-error" />,
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
            <div className={styles.headerRight}>
              <button
                className={styles.qrButton}
                onClick={() => setIsQrPopupOpen(true)}
              >
                <QrCode size={22} />
              </button>
              <SquarePen
                size={24}
                className="cursor-pointer"
                onClick={handleEditVenue}
              />
              <Icon
                src="/assets/b2b/more-icon.svg"
                alt="more"
                width={24}
                height={24}
                onClick={() => setIsMenuOpen(true)}
              />
            </div>
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
              onClick={() => {
                if (!currentVenue.isVerified) {
                  setIsVerificationDialogOpen(true);
                }
              }}
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
            {currentVenue.isPinCheckInAllowed && currentVenue.pinCode && (
              <div className={styles.pinBadge}>
                <span>{currentVenue.pinCode}</span>
              </div>
            )}
          </>
        ) : (
          <div className="bg-surface-dark h-full w-full" />
        )}
      </div>

      <div className="sm:px-4 pb-2 pt-2">
        <TabControl
          options={[
            { value: 'venue', label: t('venue') },
            { value: 'deals', label: t('deals') },
            { value: 'events', label: t('events') },
          ]}
          value={activeTab}
          onChange={setActiveTab}
        />
      </div>

      <div className="flex-1 overflow-y-auto">
        {/* Venue Tab */}
        {activeTab === 'venue' && (
          <div className={styles.content}>
            {currentVenue.tags?.length > 0 && (
              <div className={styles.section}>
                <VenueTagAccordion tagIds={currentVenue.tags.map(t => t.id)} tagsEndpoint={API_ENDPOINTS.B2B.TAGS.LIST} />
              </div>
            )}

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
                <div className={styles.fieldIcon}>
                  <Image
                    src="/assets/b2b/google-maps-icon.svg"
                    alt="Maps"
                    width={24}
                    height={30}
                  />
                </div>
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
                    <div className={styles.fieldIcon}>
                      <Image
                        src="/assets/b2b/phone-icon.svg"
                        alt="Phone"
                        width={24}
                        height={24}
                      />
                    </div>
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
                    <div className={styles.fieldIcon}>
                      <Image
                        src="/assets/b2b/website-icon.svg"
                        alt="Website"
                        width={24}
                        height={24}
                      />
                    </div>
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
                    <div className={styles.fieldIcon}>
                      <InstagramIcon />
                    </div>
                    <span className={styles.fieldText}>
                      {parseInstagramHandle(currentVenue.instagramUrl)}
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
          </div>
        )}

        {/* Deals Tab */}
        {activeTab === 'deals' && (
          <div className={styles.content}>
            {currentVenue.promos && (
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
                        <div className="flex items-center gap-3">
                          <PromoIcon
                            type={promo.type}
                            className="text-primary shrink-0"
                            size={24}
                          />
                          <div className={styles.promoBody}>{promo.body}</div>
                        </div>
                        <div className="shrink-0 p-1">
                          <MoreHorizontal size={20} className="text-secondary" />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {currentVenue.discounts && (
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
                            {discount.percent === null
                              ? t('customDiscount')
                              : `${discount.percent}%`}
                          </span>
                          {discount.loyaltyLevelId != null && (
                            <span className="text-secondary text-sm">
                              {t('loyaltyLevelTitle', {
                                level: discount.loyaltyLevelId,
                              })}
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
        )}

        {/* Events Tab */}
        {activeTab === 'events' && (
          <div className={styles.content}>
            <div className={styles.section}>
              <div className={styles.listHeader}>
                <h2 className={styles.listTitle}>{t('events')}</h2>
                <div onClick={handleAddEvent} className="cursor-pointer">
                  <Plus size={24} className="text-primary" />
                </div>
              </div>
              {isEventsLoading ? (
                <div className="flex justify-center py-8">
                  <Spinner />
                </div>
              ) : venueEvents.length === 0 ? (
                <div className="py-8 text-center text-sm text-secondary">
                  {t('noVenueEvents')}
                </div>
              ) : (
                <div className="flex flex-col gap-2">
                  {venueEvents.map((ev) => (
                    <div
                      key={ev.id}
                      onClick={(e) => {
                        const target = e.target as HTMLElement;
                        if (target.closest('button')) return;
                        handleVenueEventMenuOpen(ev.id, e);
                      }}
                      className="relative flex min-h-[56px] w-full cursor-pointer items-end overflow-hidden sm:rounded-xl bg-[#1e1e1e]"
                    >
                      {ev.coverImageUrl ? (
                        <img
                          src={ev.coverImageUrl}
                          alt={ev.title}
                          className="absolute inset-0 h-full w-full object-cover"
                        />
                      ) : null}
                      <div className="absolute inset-0 bg-black/60" />

                      <div className="relative z-10 flex w-full items-center justify-between px-3 py-3">
                        <div className="flex flex-col">
                          <span className="overflow-hidden text-sm leading-tight font-normal text-white drop-shadow-md">
                            {ev.title}
                          </span>
                          <div className="mt-0.5 flex items-center gap-2 text-xs text-white/70 drop-shadow-md">
                            <span>{new Date(ev.startDateTime).toLocaleDateString()} {new Date(ev.startDateTime).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}</span>
                            <span className="flex items-center gap-1">
                              <Users size={12} />
                              {ev.participantsCount}
                            </span>
                          </div>
                        </div>

                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleVenueEventMenuOpen(ev.id, e);
                          }}
                          className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full hover:bg-white/10"
                        >
                          <MoreHorizontal size={16} className="text-white/70" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
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

      <ModalMenu
        isOpen={isVenueEventMenuOpen}
        onClose={() => setIsVenueEventMenuOpen(false)}
        options={[
          {
            icon: <SquarePen size={24} className="text-primary" />,
            label: t('menu.editEvent'),
            onClick: handleEditVenueEvent,
          },
          {
            icon: <Trash2 size={24} className="text-error" />,
            label: t('menu.deleteEvent'),
            onClick: handleDeleteVenueEvent,
            isDestructive: true,
          },
        ]}
      />

      <ModalDialog
        isOpen={isVenueEventDeleteModalOpen}
        onClose={() => setIsVenueEventDeleteModalOpen(false)}
        message={t('deleteEventDialog.title')}
        primaryButtonText={t('deleteDialog.buttonNo')}
        secondaryButtonText={t('deleteDialog.buttonYes')}
        secondaryButtonVariant={ButtonVariant.Destructive}
        onPrimaryAction={() => setIsVenueEventDeleteModalOpen(false)}
        onSecondaryAction={handleVenueEventDeleteConfirm}
      />

      <QrPopup
        isOpen={isQrPopupOpen}
        onClose={() => setIsQrPopupOpen(false)}
        url={buildQrUrl(currentVenue.qrCode)}
      />
      <ModalDialog
        isOpen={isVerificationDialogOpen}
        onClose={() => setIsVerificationDialogOpen(false)}
        message={t.rich('verificationDialog.message', {
          email: (chunks) => (
            <a
              href="mailto:contacts@kegry.com"
              className="text-primary underline"
            >
              {chunks}
            </a>
          ),
          chat: (chunks) => (
            <a
              href="#"
              className="text-primary underline"
              onClick={(e) => {
                e.preventDefault();
                // @ts-ignore
                if (window.Tawk_API) {
                  // @ts-ignore
                  window.Tawk_API.maximize();
                }
                setIsVerificationDialogOpen(false);
              }}
            >
              {chunks}
            </a>
          ),
        })}
        primaryButtonText={t('verificationDialog.close')}
        onPrimaryAction={() => setIsVerificationDialogOpen(false)}
      />
    </div>
  );
}
