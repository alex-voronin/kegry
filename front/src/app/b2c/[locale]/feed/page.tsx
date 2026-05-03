'use client';

import { useEffect, useState, useRef, useCallback } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { Link, useRouter } from '@/i18n/routing';
import { AlertCircle, ScanQrCode } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import styles from './feed.module.css';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { CheckInManager } from '@/components/common/check-in-manager';
import { PAGE_ROUTES } from '@/lib/routes';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { useFeedStore, UserProfileDto } from './use-feed-store';
import { useNpcStore, NpcDto } from './use-npc-store';
import { useSettingsStore } from '@/app/b2c/[locale]/settings/use-settings-store';
import { toast } from 'react-toastify';
import { InviteExpiration, expiresAtToWhen } from '@/lib/invite-utils';
import { useFcmStore } from '@/lib/fcm/store';
import { NotificationType, FcmPayload } from '@/lib/fcm/types';
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
import { useQrCheckIn } from '@/app/b2c/[locale]/venues/use-qr-check-in';
import { FEED_VISITS_TO_SHOW_REFERRAL_PROMPT } from '@/lib/constants';
import { buildQrUrl } from '@/lib/qr-utils';

// --- Discriminated union for feed items ---

type FeedItem =
  | { kind: 'user'; profile: UserProfileDto }
  | { kind: 'npc'; npc: NpcDto };

// --- Sub-components ---

function FeedHeader({
  router,
  unseenCount,
  showPermissionsBadge,
  isInMeeting,
  onboardingRequired,
  showQrScan,
  onQrScanClick,
}: {
  router: ReturnType<typeof useRouter>;
  unseenCount: number;
  showPermissionsBadge: boolean;
  isInMeeting: boolean;
  onboardingRequired: boolean;
  showQrScan: boolean;
  onQrScanClick: () => void;
}) {
  return (
    <Header
      left={
        <Icon
          src="/assets/common/profile-icon.svg"
          alt="profile"
          width={30}
          height={32}
          priority
          onClick={() => router.push(PAGE_ROUTES.PROFILE)}
        >
          {showPermissionsBadge && !onboardingRequired && (
            <AlertCircle
              size={16}
              strokeWidth={3}
              className={styles.permissionsIcon}
            />
          )}
        </Icon>
      }
      right={
        <div className={styles.headerRightSideIconsWrapper}>
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
          {isInMeeting ? (
            <Icon
              src="/assets/feed/chat-icon.svg"
              alt="chat"
              width={28}
              height={30}
              priority
              onClick={() => router.push(PAGE_ROUTES.CHAT)}
            />
          ) : (
            <Icon
              src="/assets/feed/notifications-icon.svg"
              alt="notifications"
              width={28}
              height={30}
              priority
              onClick={() => router.push(PAGE_ROUTES.NOTIFICATIONS)}
            >
              {unseenCount > 0 && (
                <span className={styles.badge}>
                  {unseenCount > 99 ? '99+' : unseenCount}
                </span>
              )}
            </Icon>
          )}
        </div>
      }
    />
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
    unseenNotificationCount,
    fetchUnseenNotificationCount,
    setUnseenNotificationCount,
    availableInvitesCount,
    isInMeeting,
    isPerformingAction,
  } = useFeedStore();
  const requiredAction = useAuthStore((state) => state.requiredAction);
  const { npc, monolog, fetchNpc, inviteNpc, dismissNpc } = useNpcStore();
  const [showPermissionsBadge, setShowPermissionsBadge] = useState(false);
  const [isLocationDenied, setIsLocationDenied] = useState(false);
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
  const {
    isCodePopupOpen,
    setIsCodePopupOpen,
    handleQrCode,
    handleCodeSubmit,
  } = useQrCheckIn({
    onSuccess: handleCheckInSuccess,
    onError: handleCheckInError,
  });

  const handleIncognitoCheckInClick = async () => {
    const { locationAllowed } = await checkPermissionStatus();
    if (!locationAllowed) {
      // Trigger the native browser permission prompt if not granted yet
      navigator.geolocation.getCurrentPosition(
        () => setIsScannerOpen(true),
        () => setIsScannerOpen(true),
        { timeout: 500 },
      );
    } else {
      setIsScannerOpen(true);
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

  const autoChatRedirect = searchParams.get('auto-chat-redirect');
  useEffect(() => {
    let isMounted = true;
    const initializeFeed = async () => {
      if (!hasHydrated) return;

      // 1. Handle QR code from local storage early
      const qrCode = localStorage.getItem('qr_code_to_check_in');
      if (qrCode) {
        localStorage.removeItem('qr_code_to_check_in');
        const qrUrl = buildQrUrl(qrCode);
        await handleQrCode(qrUrl);
        return;
      }

      if (requiredAction === RequiredAction.OnboardingCompletion) {
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
        }
        setIsLocationDenied(false);

        // 3. Fetch feed data and status
        const [status] = await Promise.all([
          fetchFeedStatus(),
          fetchFeed(),
          fetchUnseenNotificationCount(),
          fetchNpc(),
        ]);

        if (!isMounted) return;

        // 4. Handle redirects (Check-in/Meeting/Rating)
        if (status.isInMeeting) {
          useFeedStore.setState({ isInMeeting: true });
          if (autoChatRedirect !== 'false') {
            router.push(PAGE_ROUTES.CHAT);
            return;
          }
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
    fetchUnseenNotificationCount,
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

  const { registerForegroundHandler, unregisterForegroundHandler } =
    useFcmStore();

  useEffect(() => {
    const handleNotification = (payload: FcmPayload) => {
      const type = payload?.data?.type;
      if (type === NotificationType.USER_INVITED) {
        setUnseenNotificationCount((current) => current + 1);
      }
    };

    registerForegroundHandler(handleNotification);
    return () => unregisterForegroundHandler(handleNotification);
  }, [
    registerForegroundHandler,
    unregisterForegroundHandler,
    setUnseenNotificationCount,
  ]);

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
    if (currentItem?.kind !== 'user') return;
    router.push(PAGE_ROUTES.USER_PROFILE(currentItem.profile.id));
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
                  <Link
                    href={PAGE_ROUTES.ONBOARDING}
                    className={styles.emptyStateLink}
                  >
                    {chunks}
                  </Link>
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
        {/* eslint-disable-next-line jsx-a11y/click-events-have-key-events, jsx-a11y/no-static-element-interactions */}
        <div onClick={(e) => e.stopPropagation()}>
          <FeedHeader
            router={router}
            unseenCount={unseenNotificationCount}
            showPermissionsBadge={showPermissionsBadge}
            isInMeeting={isInMeeting}
            onboardingRequired={
              requiredAction === RequiredAction.OnboardingCompletion
            }
            showQrScan={requiredAction === RequiredAction.None}
            onQrScanClick={() => setIsScannerOpen(true)}
          />
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
        <CheckInManager
          isScannerOpen={isScannerOpen}
          onScannerOpenChange={setIsScannerOpen}
          isCodePopupOpen={isCodePopupOpen}
          onCodePopupOpenChange={setIsCodePopupOpen}
          handleQrCode={handleQrCode}
          handleCodeSubmit={handleCodeSubmit}
        />
      </div>
    </div>
  );
}
