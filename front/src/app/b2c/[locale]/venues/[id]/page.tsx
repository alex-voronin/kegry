'use client';

import { useEffect, useState, useCallback } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter, usePathname } from '@/i18n/routing';
import { useParams, useSearchParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { SegmentedControl } from '@/components/ui/segmented-control';
import { useVenueStore } from './use-venue-store';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { toast } from 'react-toastify';
import { ScanQrCode, Link } from 'lucide-react';
import styles from './venue-detail.module.css';
import { PinpadIcon } from '@/components/icons/pinpad-icon';
import { useSafeBack } from '@/hooks/use-safe-back';
import { CheckInManager } from '@/components/common/check-in-manager';
import { CheckInAnimation } from '../_components/check-in-animation';
import { useQrCheckIn } from '../use-qr-check-in';
import { DetailsTab } from './_components/details-tab';
import { EventsTab } from './_components/events-tab';
import { InstagramIcon } from './_components/instagram-icon';
import apiClient from '@/lib/api-client';

const PIN_CODE_ONLY_CHECKIN_ENABLED = false;

export default function VenueDetailScreen() {
  const router = useRouter();
  const pathname = usePathname();
  const params = useParams();
  const searchParams = useSearchParams();
  const venueId = params.id as string;
  const t = useTranslations('screens.venueDetail');
  const tPopup = useTranslations('components.checkInCodePopup');

  const {
    currentVenue,
    isLoading,
    venueEvents,
    totalEventCount,
    isEventsLoading,
    fetchVenue,
    fetchVenueEvents: fetchVenueEventsStore,
    updateEventParticipation,
  } = useVenueStore();
  const [isInitializing, setIsInitializing] = useState(
    () => currentVenue?.venue.id !== venueId,
  );
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isHelpDialogOpen, setIsHelpDialogOpen] = useState(false);
  const [checkInVenueId, setCheckInVenueId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState('details');
  const [isEventMenuOpen, setIsEventMenuOpen] = useState(false);
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null);
  const hasMoreEvents = venueEvents.length < totalEventCount;

  const handleCheckInSuccess = useCallback((id: string) => {
    setCheckInVenueId(id);
  }, []);

  const handleCheckInError = useCallback(
    (venueId: string | null) => {
      setIsScannerOpen(false);
      if (venueId) {
        router.push(PAGE_ROUTES.VENUE_DETAIL(venueId));
      }
    },
    [router],
  );

  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const {
    isCodePopupOpen,
    setIsCodePopupOpen,
    showCheckIn,
    setShowCheckIn,
    handleQrCode,
    handleCodeSubmit,
    showLocationRequiredDialog,
    dismissLocationRequiredDialog,
    setPendingQrCode,
    codeDescription,
    setCodeDescription,
  } = useQrCheckIn({
    onSuccess: handleCheckInSuccess,
    onError: handleCheckInError,
  });

  const { safeBack } = useSafeBack();

  useEffect(() => {
    if (searchParams.get('animate-check-in') === 'true') {
      setShowCheckIn(true);
      router.replace(pathname);
    }
    const tabParam = searchParams.get('tab');
    if (tabParam === 'events') {
      setActiveTab('events');
      router.replace(pathname);
    }
  }, [searchParams, router, pathname]);

  useEffect(() => {
    const loadVenue = async () => {
      // If we don't have the correct venue, show initializing loader
      if (useVenueStore.getState().currentVenue?.venue.id !== venueId) {
        setIsInitializing(true);
      }
      try {
        await fetchVenue(venueId);
        fetchVenueEventsStore(venueId, 0);
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

  const handleEventMenuOpen = useCallback((eventId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setSelectedEventId(eventId);
    setIsEventMenuOpen(true);
  }, []);

  const handleJoinEvent = useCallback(async () => {
    if (!selectedEventId) return;
    setIsEventMenuOpen(false);
    try {
      await apiClient.post(API_ENDPOINTS.EVENTS.JOIN(selectedEventId));
      updateEventParticipation(selectedEventId, true);
    } catch {
      toast.error('Failed to join event');
    }
  }, [selectedEventId, updateEventParticipation]);

  const handleLeaveEvent = useCallback(async () => {
    if (!selectedEventId) return;
    setIsEventMenuOpen(false);
    try {
      await apiClient.post(API_ENDPOINTS.EVENTS.LEAVE(selectedEventId));
      updateEventParticipation(selectedEventId, false);
    } catch {
      toast.error('Failed to leave event');
    }
  }, [selectedEventId, updateEventParticipation]);

  const handleViewEvent = useCallback(() => {
    if (!selectedEventId) return;
    setIsEventMenuOpen(false);
    router.push(PAGE_ROUTES.MEETUP_DETAIL(selectedEventId));
  }, [selectedEventId, router]);

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
          right={
            <div className="flex items-center gap-3">
              {PIN_CODE_ONLY_CHECKIN_ENABLED && venue.isPinCheckInAllowed && (
                <Icon
                  onClick={() => {
                    setPendingQrCode(venue.qrCode);
                    setCodeDescription(tPopup('descriptionManual'));
                    setIsCodePopupOpen(true);
                  }}
                >
                  <PinpadIcon size={24} className="text-white" />
                </Icon>
              )}
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
          <div className={styles.section}>
            <h1 className={styles.companyName}>{company.name}</h1>
          </div>

          <div className={styles.segmentedControlWrapper}>
            <SegmentedControl
              options={[
                { value: 'details', label: t('tabDetails'), width: 'auto' },
                { value: 'events', label: t('tabEvents'), width: 'auto' },
              ]}
              value={activeTab}
              onChange={setActiveTab}
              stretchItems
            />
          </div>

          {/* Details Tab */}
          {activeTab === 'details' && (
            <DetailsTab
              venue={venue}
              company={company}
              loyaltyLevel={loyaltyLevel}
              venueId={venueId}
              t={t}
              onHelpOpen={() => setIsHelpDialogOpen(true)}
            />
          )}

          {/* Events Tab */}
          {activeTab === 'events' && (
            <EventsTab
              venueEvents={venueEvents}
              isEventsLoading={isEventsLoading}
              hasMoreEvents={hasMoreEvents}
              onLoadMore={() => fetchVenueEventsStore(venueId, venueEvents.length)}
              onEventMenuOpen={handleEventMenuOpen}
              onEmpty={() => t('noVenueEvents')}
            />
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

      <ModalMenu
        isOpen={isEventMenuOpen}
        onClose={() => setIsEventMenuOpen(false)}
        options={(() => {
          const ev = venueEvents.find((e) => e.id === selectedEventId);
          return [
            {
              label: ev?.isJoined ? 'Leave' : 'Join',
              onClick: ev?.isJoined ? handleLeaveEvent : handleJoinEvent,
            },
            {
              label: 'View',
              onClick: handleViewEvent,
            },
          ];
        })()}
      />

      <CheckInManager
        isScannerOpen={isScannerOpen}
        onScannerOpenChange={setIsScannerOpen}
        isCodePopupOpen={isCodePopupOpen}
        onCodePopupOpenChange={setIsCodePopupOpen}
        handleQrCode={handleQrCode}
        handleCodeSubmit={handleCodeSubmit}
        codeDescription={codeDescription}
        showLocationRequiredDialog={showLocationRequiredDialog}
        onDismissLocationRequiredDialog={dismissLocationRequiredDialog}
      />

      <ModalDialog
        isOpen={isHelpDialogOpen}
        onClose={() => setIsHelpDialogOpen(false)}
        message={
          loyaltyLevel?.isCustomDiscount
            ? t('helpDialogCustomDiscount')
            : t('helpDialog')
        }
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
