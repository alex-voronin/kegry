'use client';

import { useEffect, useState, useRef, useCallback } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { Link, useRouter } from '@/i18n/routing';
import { ScanQrCode } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import styles from './feed.module.css';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { CheckInManager } from '@/components/common/check-in-manager';
import { PAGE_ROUTES } from '@/lib/routes';
import { MapFeedToggle } from '@/components/common/map-feed-toggle';
import { NotificationsButton } from '@/components/common/notifications-button';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { useFeedStore, UserProfileDto } from './use-feed-store';
import { useNpcStore, NpcDto } from './use-npc-store';
import { useSettingsStore } from '@/app/b2c/[locale]/settings/use-settings-store';
import { toast } from 'react-toastify';
import { InviteExpiration, expiresAtToWhen } from '@/lib/invite-utils';
import {
  checkIfPermissionsDenied,
  checkPermissionStatus,
} from '@/lib/permissions';
import {
  SendLocationWithRateLimit,
  getLastSentTimestamp,
} from '@/lib/use-background-location';
import {
  UserFeedBody,
  UserFeedFooter,
  AvailableInvites,
  WhenSelector,
} from './_components/user-feed-card';
import { NpcFeedBody, NpcFeedFooter } from './_components/npc-feed-card';
import { useSidebar, Sidebar } from '@/components/b2c/Sidebar';
import { BurgerButton } from '@/components/common/burger-button';
import { usePendingApprovalsStore } from '@/components/b2c/use-pending-approvals-store';
import { useQrCheckIn } from '@/app/b2c/[locale]/venues/use-qr-check-in';
import { FEED_VISITS_TO_SHOW_REFERRAL_PROMPT } from '@/lib/constants';
import { processStoredQrCode } from '@/lib/qr-utils';

// --- Discriminated union for feed items ---

type FeedItem =
  | { kind: 'user'; profile: UserProfileDto }
  | { kind: 'npc'; npc: NpcDto };

// --- Sub-components ---

function FeedHeader({
  isInMeeting,
  onboardingRequired,
  showQrScan,
  onQrScanClick,
}: {
  isInMeeting: boolean;
  onboardingRequired: boolean;
  showQrScan: boolean;
  onQrScanClick: () => void;
}) {
  const router = useRouter();
  return (
    <div className={styles.feedTopBar}>
      <div className={styles.feedTopBarItem} />
      <div className={styles.feedIconPill}>
        <div className={styles.feedIconPillIcons}>
          {showQrScan && (
            <Icon width={32} height={32} onClick={onQrScanClick}>
              <ScanQrCode className="text-primary h-full w-full" />
            </Icon>
          )}
          <Icon
            src="/assets/venue/venue-icon.svg"
            alt="venues"
            width={30}
            height={30}
            priority
            onClick={() => router.push(PAGE_ROUTES.VENUES)}
          />
          {!isInMeeting && !onboardingRequired && (
            <Icon
              src="/assets/feed/filters-icon.svg"
              alt="filters"
              width={32}
              height={32}
              priority
              onClick={() => router.push(PAGE_ROUTES.FEED_FILTERS)}
            />
          )}
          {isInMeeting && (
            <Icon
              src="/assets/feed/chat-icon.svg"
              alt="chat"
              width={28}
              height={30}
              priority
              onClick={() => router.push(PAGE_ROUTES.CHAT)}
            />
          )}
          <NotificationsButton />
        </div>
      </div>
    </div>
  );
}

// --- Main Screen ---

export default function FeedScreen() {
  const tFeed = useTranslations('screens.feed');
  const tCommon = useTranslations('common');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const router = useRouter();
  const searchParams = useSearchParams();
  const {
    feed,
    fetchFeedStatus,
    fetchFeed,
    inviteUser,
    getInviteExpiration,
    skipUser,
    ignoreUser,
    isLoading,
    hasHydrated,
    availableInvitesCount,
    isInMeeting,
    isPerformingAction,
  } = useFeedStore();
  const requiredAction = useAuthStore((state) => state.requiredAction);
  const { npc, monolog, fetchNpc, inviteNpc, dismissNpc } = useNpcStore();
  const [showPermissionsBadge, setShowPermissionsBadge] = useState(false);
  const pendingTotal = usePendingApprovalsStore((s) => s.total);
  const [isLocationDenied, setIsLocationDenied] = useState(false);
  const {
    isOpen: isSidebarOpen,
    open: openSidebar,
    close: closeSidebar,
  } = useSidebar();
  const handleCheckInSuccess = useCallback(
    (venueId: string) => {
      // We use requestAnimationFrame() to avoid a race condition between the Feed's state update (closing scanner)
      // and the router's push navigation. Without this delay, Next.js may perform a 'replace'
      // instead of a 'push', leading to a shallow history stack where going 'back' fails.
      requestAnimationFrame(() => {
        router.push(
          `${PAGE_ROUTES.VENUE_DETAIL(venueId)}?animate-check-in=true`,
        );
      });
    },
    [router],
  );

  const handleCheckInError = useCallback(
    (venueId: string | null) => {
      setIsScannerOpen(false);
      if (venueId) {
        // We use requestAnimationFrame() to avoid a race condition between the Feed's state update (closing scanner)
        // and the router's push navigation. Without this delay, Next.js may perform a 'replace'
        // instead of a 'push', leading to a shallow history stack where going 'back' fails.
        requestAnimationFrame(() => {
          router.push(PAGE_ROUTES.VENUE_DETAIL(venueId));
        });
      } else {
        router.refresh();
      }
    },
    [router],
  );

  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const [isFirstCheckInDialogOpen, setIsFirstCheckInDialogOpen] =
    useState(false);

  const handleQrScanClick = useCallback(() => {
    const hasSeenDialog = localStorage.getItem('has_seen_first_checkin_dialog');
    if (!hasSeenDialog) {
      setIsFirstCheckInDialogOpen(true);
    } else {
      setIsScannerOpen(true);
    }
  }, []);

  const handleFirstCheckInOk = useCallback(() => {
    localStorage.setItem('has_seen_first_checkin_dialog', 'true');
    setIsFirstCheckInDialogOpen(false);
    setIsScannerOpen(true);
  }, []);

  const {
    isCodePopupOpen,
    setIsCodePopupOpen,
    handleQrCode,
    handleCodeSubmit,
    showLocationRequiredDialog,
    dismissLocationRequiredDialog,
  } = useQrCheckIn({
    onSuccess: handleCheckInSuccess,
    onError: handleCheckInError,
  });

  const handleIncognitoCheckInClick = async () => {
    const { locationAllowed } = await checkPermissionStatus();
    if (!locationAllowed) {
      // Trigger the native browser permission prompt if not granted yet
      navigator.geolocation.getCurrentPosition(
        () => handleQrScanClick(),
        () => handleQrScanClick(),
        { timeout: 500 },
      );
    } else {
      handleQrScanClick();
    }
  };
  const [when, setWhen] = useState<InviteExpiration>(InviteExpiration.Today);
  const [expirationPopup, setExpirationPopup] = useState<{
    isOpen: boolean;
    suggestedWhen: InviteExpiration;
  }>({ isOpen: false, suggestedWhen: InviteExpiration.Today });
  const [profilesCountToNpc, setProfilesCountToNpc] = useState<number | null>(
    null,
  );
  const [isReferralPromptOpen, setIsReferralPromptOpen] = useState(false);
  const [isOnboardingWarningOpen, setIsOnboardingWarningOpen] = useState(false);

  const checkPermissions = useCallback(async () => {
    const isDenied = await checkIfPermissionsDenied();
    setShowPermissionsBadge(isDenied);
  }, []);

  useEffect(() => {
    checkPermissions();

    // Re-check when window gains focus
    window.addEventListener('focus', checkPermissions);
    return () => window.removeEventListener('focus', checkPermissions);
  }, [checkPermissions]);

  const distanceUnits = useSettingsStore((state) => state.distanceUnits);

  const isNpcTurn = npc && (profilesCountToNpc === 0 || feed.length === 0);
  const currentItem: FeedItem | null = isNpcTurn
    ? { kind: 'npc', npc }
    : feed[0]
      ? { kind: 'user', profile: feed[0] }
      : null;

  // Preload cover images for the next 2 profiles each time the current profile changes
  useEffect(() => {
    feed.slice(1, 3).forEach((profile) => {
      if (profile.coverImageUrl) {
        const img = new window.Image();
        img.src = profile.coverImageUrl;
      }
    });
  }, [currentItem?.kind === 'user' ? currentItem.profile.id : null]); // eslint-disable-line react-hooks/exhaustive-deps

  const [isInitialized, setIsInitialized] = useState(false);
  const locationToastRef = useRef<ReturnType<typeof toast.loading> | null>(
    null,
  );

  useEffect(() => {
    let isMounted = true;
    const initializeFeed = async () => {
      if (!hasHydrated) return;

      if (await processStoredQrCode(handleQrCode)) return;

      if (requiredAction !== RequiredAction.None) {
        return;
      }

      try {
        // 2. Send location first
        const isFirstLocationSubmission = getLastSentTimestamp() === 0;

        if (isFirstLocationSubmission) {
          locationToastRef.current = toast.loading(
            tCommon('toast.gettingLocation'),
          );
        }

        const locationResult = await SendLocationWithRateLimit(
          isFirstLocationSubmission ? 30000 : 15000,
        );

        if (!isMounted) return;

        if (locationToastRef.current !== null) {
          toast.dismiss(locationToastRef.current);
          locationToastRef.current = null;
        }

        await checkPermissions(); // Sync the permissions badge after location attempt
        if (locationResult === 'denied') {
          setIsLocationDenied(true);
          return;
        } else if (locationResult === 'error') {
          const { locationAllowed } = await checkPermissionStatus();
          if (!locationAllowed) {
            setIsLocationDenied(true);
            return;
          }
        }
        setIsLocationDenied(false);

        // 3. Fetch feed data and status
        const [status] = await Promise.all([
          fetchFeedStatus(),
          fetchFeed(),
          fetchNpc(),
        ]);

        if (!isMounted) return;

        // 4. Handle redirects (Check-in/Rating)
        if (status.isInMeeting) {
          useFeedStore.setState({ isInMeeting: true });
        }

        if (status.userIdToRate) {
          router.push(PAGE_ROUTES.RATE_USER(status.userIdToRate));
          return;
        }

        // 5. If we reached here, it's a "true" feed display. Count the visit.
        const referralShown = localStorage.getItem('referral_prompt_shown');

        if (!referralShown) {
          const currentCount = Number(
            localStorage.getItem('feed_visit_count') || '0',
          );

          const newCount = currentCount + 1;
          localStorage.setItem('feed_visit_count', String(newCount));

          if (newCount === FEED_VISITS_TO_SHOW_REFERRAL_PROMPT) {
            setIsReferralPromptOpen(true);
            localStorage.setItem('referral_prompt_shown', 'true');
            localStorage.removeItem('feed_visit_count');
          }
        }
        // 6. Handle NPC logic
        const currentNpc = useNpcStore.getState().npc;
        const currentFeed = useFeedStore.getState().feed;
        if (currentNpc && currentFeed.length > 0) {
          const maxIdx = Math.min(3, currentFeed.length);
          setProfilesCountToNpc(Math.floor(Math.random() * (maxIdx + 1)));
        } else if (currentNpc) {
          setProfilesCountToNpc(0);
        }
      } catch (err: any) {
        toast.error(err.message);
      } finally {
        setIsInitialized(true);
      }
    };

    initializeFeed();

    return () => {
      isMounted = false;

      if (locationToastRef.current !== null) {
        toast.dismiss(locationToastRef.current);
        locationToastRef.current = null;
      }
    };
  }, [
    hasHydrated,
    fetchFeedStatus,
    fetchFeed,
    fetchNpc,
    router,
    handleQrCode,
    checkPermissions,
  ]);

  const handleCloseInvitePrompt = () => {
    localStorage.setItem('has_seen_invite_friends_prompt', 'true');
    setIsReferralPromptOpen(false);
  };

  const handleGoToInvite = () => {
    localStorage.setItem('has_seen_invite_friends_prompt', 'true');
    setIsReferralPromptOpen(false);
    router.push(`${PAGE_ROUTES.FRIENDS_AND_FOES}?share=true`);
  };

  const showLoading = isLoading || !hasHydrated || !isInitialized;

  const decreaseProfilesCountToNpc = () => {
    if (profilesCountToNpc !== null && profilesCountToNpc > 0) {
      setProfilesCountToNpc(profilesCountToNpc - 1);
    }
  };

  // --- User action handlers ---
  const handleIgnore = async () => {
    if (currentItem?.kind === 'npc') {
      dismissNpc();
      setProfilesCountToNpc(null);
      return;
    }
    if (currentItem?.kind !== 'user') return;
    try {
      await ignoreUser(currentItem.profile.id);
      decreaseProfilesCountToNpc();
    } catch (err: any) {
      toast.error(err.message);
    }
  };

  const handleInvite = async () => {
    if (availableInvitesCount <= 0) {
      toast.error(tFeed('toast.noInvites'));
      return;
    }

    if (currentItem?.kind === 'npc') {
      await inviteNpc(currentItem.npc.id);
      return;
    }
    if (currentItem?.kind !== 'user') return;
    try {
      const incomingExpiration = await getInviteExpiration(
        currentItem.profile.id,
      );
      if (incomingExpiration) {
        const theirWhen = expiresAtToWhen(incomingExpiration);
        if (theirWhen && theirWhen !== when) {
          setExpirationPopup({ isOpen: true, suggestedWhen: theirWhen });
          return;
        }
      }
      await inviteUser(currentItem.profile.id, when);
      decreaseProfilesCountToNpc();
    } catch (err: any) {
      toast.error(err.message);
    }
  };

  const handleAcceptSuggestedExpiration = async () => {
    if (currentItem?.kind !== 'user') return;
    try {
      await inviteUser(currentItem.profile.id, expirationPopup.suggestedWhen);
      decreaseProfilesCountToNpc();
    } catch (err: any) {
      toast.error(err.message);
    } finally {
      setExpirationPopup({ ...expirationPopup, isOpen: false });
    }
  };

  const handleSkip = async () => {
    if (currentItem?.kind === 'npc') {
      dismissNpc();
      setProfilesCountToNpc(null);
      return;
    }
    if (currentItem?.kind !== 'user') return;
    try {
      await skipUser(currentItem.profile.id);
      decreaseProfilesCountToNpc();
    } catch (err: any) {
      toast.error(err.message);
    }
  };

  const handleProfileClick = () => {
    if (isInMeeting) {
      router.push(PAGE_ROUTES.CHAT);
      return;
    }
    if (currentItem?.kind === 'npc') {
      if (monolog) {
        handleSkip();
      } else {
        handleInvite();
      }
      return;
    }
    if (currentItem?.kind !== 'user') return;
    router.push(PAGE_ROUTES.USER_PROFILE(currentItem.profile.id));
  };

  const handleOnboardingClick = async (e: React.MouseEvent) => {
    e.stopPropagation();
    const isDenied = await checkIfPermissionsDenied();
    if (isDenied) {
      setIsOnboardingWarningOpen(true);
    } else {
      router.push(PAGE_ROUTES.ONBOARDING);
    }
  };

  // --- Render ---

  const renderBody = () => {
    if (requiredAction === RequiredAction.OnboardingCompletion) {
      return (
        <div className={styles.emptyState}>
          <div className={styles.onboardingContent}>
            <div className={styles.incognitoWrapper}>
              <Image
                src="/assets/feed/incognito.webp"
                alt="incognito"
                width={300}
                height={300}
                className="object-cover"
              />
            </div>
            <p className={styles.emptyStateText}>
              {tFeed.rich('onboardingPlaceholder', {
                link: (chunks) => (
                  <button
                    className={styles.emptyStateLink}
                    onClick={handleOnboardingClick}
                  >
                    {chunks}
                  </button>
                ),
              })}
            </p>
          </div>
          <button
            className={styles.qrScanButton}
            onClick={() => handleIncognitoCheckInClick()}
          >
            <ScanQrCode className={styles.qrScanIcon} />
            <span className={styles.qrScanText}>{tFeed('scanQr')}</span>
          </button>
        </div>
      );
    }

    if (showLoading && !currentItem && !isInMeeting) {
      return (
        <div className={styles.loadingContainer}>
          <Spinner />
        </div>
      );
    }

    if (isInMeeting) {
      return (
        <div className={styles.emptyState}>
          <Image
            src="/assets/feed/go-to-chat.webp"
            alt="in meeting"
            width={400}
            height={370}
            className={styles.emptyStateImage}
          />
          <p className={styles.emptyStateText}>{tFeed('inMeeting')}</p>
        </div>
      );
    }

    if (isLocationDenied) {
      return (
        <div className={styles.emptyState}>
          <Image
            src="/assets/feed/geolocation-disabled-placeholder.webp"
            alt="location required"
            width={300}
            height={300}
            className={styles.emptyStateImage}
          />
          <p className={styles.emptyStateText}>
            {tFeed.rich('locationRequired', {
              link: (chunks) => (
                <Link
                  href={PAGE_ROUTES.PERMISSIONS}
                  className={styles.emptyStateLink}
                >
                  {chunks}
                </Link>
              ),
            })}
          </p>
        </div>
      );
    }

    if (!currentItem) {
      return (
        <div className={styles.emptyState}>
          <Image
            src="/assets/feed/no-profiles-placeholder.webp"
            alt="no profiles"
            width={300}
            height={300}
            className={styles.emptyStateImage}
          />
          <p className={styles.emptyStateText}>
            {tFeed.rich('noProfiles', {
              link: (chunks) => (
                <Link
                  href={PAGE_ROUTES.FEED_FILTERS}
                  className={styles.emptyStateLink}
                >
                  {chunks}
                </Link>
              ),
            })}
          </p>
        </div>
      );
    }

    if (currentItem.kind === 'npc') {
      return <NpcFeedBody npc={currentItem.npc} monolog={monolog} />;
    }

    return (
      <UserFeedBody
        currentProfile={currentItem.profile}
        isLoading={showLoading}
        distanceUnits={distanceUnits}
        tFeed={tFeed}
        tCommon={tCommon}
        tHobbies={tHobbies}
      />
    );
  };

  const renderFooter = () => {
    if (
      isInMeeting ||
      isLocationDenied ||
      !currentItem ||
      requiredAction === RequiredAction.OnboardingCompletion
    )
      return null;

    if (currentItem.kind === 'npc') {
      return (
        <NpcFeedFooter
          onInvite={handleInvite}
          onDismiss={handleSkip}
          hasMonolog={!!monolog}
          isPerformingAction={isPerformingAction}
        />
      );
    }

    return (
      <>
        <WhenSelector value={when} onChange={setWhen} tFeed={tFeed} />
        <AvailableInvites count={availableInvitesCount} />
        <UserFeedFooter
          onIgnore={handleIgnore}
          onInvite={handleInvite}
          onSkip={handleSkip}
          isInviteDisabled={availableInvitesCount == 0}
          isPerformingAction={isPerformingAction}
        />
      </>
    );
  };

  return (
    <div className={styles.wrapper}>
      <div className={styles.content} onClick={handleProfileClick}>
        <div onClick={(e) => e.stopPropagation()}>
          <FeedHeader
            isInMeeting={isInMeeting}
            onboardingRequired={
              requiredAction === RequiredAction.OnboardingCompletion
            }
            showQrScan={requiredAction === RequiredAction.None}
            onQrScanClick={handleQrScanClick}
          />
        </div>

        <div onClick={(e) => e.stopPropagation()}>
          <div className={styles.leftControls}>
            <BurgerButton
              onOpen={openSidebar}
              hasRestrictedPermissions={
                showPermissionsBadge &&
                requiredAction !== RequiredAction.OnboardingCompletion
              }
              pendingApprovalsCount={pendingTotal}
            />
            <MapFeedToggle activeView="feed" />
          </div>
        </div>

        {renderBody()}
        {renderFooter()}
        <ModalDialog
          isOpen={expirationPopup.isOpen}
          onClose={() =>
            setExpirationPopup({ ...expirationPopup, isOpen: false })
          }
          message={tFeed('inviteExpirationDialog.message', {
            when: tFeed(expirationPopup.suggestedWhen).toLowerCase(),
          })}
          primaryButtonText={tFeed('inviteExpirationDialog.decline')}
          onPrimaryAction={() =>
            setExpirationPopup({ ...expirationPopup, isOpen: false })
          }
          secondaryButtonText={tFeed('inviteExpirationDialog.accept', {
            when: tFeed(expirationPopup.suggestedWhen).toLowerCase(),
          })}
          onSecondaryAction={handleAcceptSuggestedExpiration}
        />
        <ModalDialog
          isOpen={isReferralPromptOpen}
          onClose={handleCloseInvitePrompt}
          message={tFeed('invitePrompt.message')}
          primaryButtonText={tFeed('invitePrompt.later')}
          onPrimaryAction={handleCloseInvitePrompt}
          secondaryButtonText={tFeed('invitePrompt.letsGo')}
          onSecondaryAction={handleGoToInvite}
        />
        <ModalDialog
          isOpen={isOnboardingWarningOpen}
          onClose={() => setIsOnboardingWarningOpen(false)}
          message={tFeed('onboardingWarningDialog.message')}
          primaryButtonText={tFeed('onboardingWarningDialog.cancel')}
          onPrimaryAction={() => setIsOnboardingWarningOpen(false)}
          secondaryButtonText={tFeed('onboardingWarningDialog.continue')}
          onSecondaryAction={() => {
            setIsOnboardingWarningOpen(false);
            router.push(PAGE_ROUTES.ONBOARDING);
          }}
        />
        <ModalDialog
          isOpen={isFirstCheckInDialogOpen}
          onClose={handleFirstCheckInOk}
          message={tFeed('firstCheckInDialog.message')}
          primaryButtonText={tFeed('firstCheckInDialog.ok')}
          onPrimaryAction={handleFirstCheckInOk}
        />
        <CheckInManager
          isScannerOpen={isScannerOpen}
          onScannerOpenChange={setIsScannerOpen}
          isCodePopupOpen={isCodePopupOpen}
          onCodePopupOpenChange={setIsCodePopupOpen}
          handleQrCode={handleQrCode}
          handleCodeSubmit={handleCodeSubmit}
          showLocationRequiredDialog={showLocationRequiredDialog}
          onDismissLocationRequiredDialog={dismissLocationRequiredDialog}
        />

        <div onClick={(e) => e.stopPropagation()}>
          <Sidebar
            isOpen={isSidebarOpen}
            onClose={closeSidebar}
            onCheckIn={handleQrScanClick}
            showPermissionsBadge={
              showPermissionsBadge &&
              requiredAction !== RequiredAction.OnboardingCompletion
            }
            isInMeeting={isInMeeting}
          />
        </div>
      </div>
    </div>
  );
}
