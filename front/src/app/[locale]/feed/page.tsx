'use client';

import { useEffect, useState, useRef } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSearchParams } from 'next/navigation';
import styles from './feed.module.css';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { PAGE_ROUTES } from '@/lib/routes';
import { useFeedStore, UserProfileDto } from '@/store/use-feed-store';
import { useNpcStore, NpcDto } from '@/store/use-npc-store';
import { useSettingsStore } from '@/store/use-settings-store';
import { toast } from 'react-toastify';
import { InviteExpiration, expiresAtToWhen } from '@/lib/invite-utils';
import { useFcmStore } from '@/lib/fcm/store';
import { NotificationType, FcmPayload } from '@/lib/fcm/types';
import { checkIfPermissionsDenied } from '@/lib/permissions';
import { SendLocationWithRateLimit } from '@/lib/use-background-location';
import {
  UserFeedBody,
  UserFeedFooter,
  AvailableInvites,
  WhenSelector,
} from './_components/user-feed-card';
import { NpcFeedBody, NpcFeedFooter } from './_components/npc-feed-card';

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
}: {
  router: ReturnType<typeof useRouter>;
  unseenCount: number;
  showPermissionsBadge: boolean;
  isInMeeting: boolean;
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
          {showPermissionsBadge && (
            <span className={styles.permissionsBadge}></span>
          )}
        </Icon>
      }
      right={
        <div className={styles.headerRightSideIconsWrapper}>
          {!isInMeeting && (
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
    isActionProcessing,
  } = useFeedStore();
  const { npc, monolog, fetchNpc, inviteNpc, dismissNpc } = useNpcStore();
  const [showPermissionsBadge, setShowPermissionsBadge] = useState(false);
  const [isLocationDenied, setIsLocationDenied] = useState(false);
  const [when, setWhen] = useState<InviteExpiration>(InviteExpiration.Today);
  const [expirationPopup, setExpirationPopup] = useState<{
    isOpen: boolean;
    suggestedWhen: InviteExpiration;
  }>({ isOpen: false, suggestedWhen: InviteExpiration.Today });
  const [profilesCountToNpc, setProfilesCountToNpc] = useState<number | null>(
    null,
  );
  const [isInvitePromptOpen, setIsInvitePromptOpen] = useState(false);

  useEffect(() => {
    const checkPermissions = async () => {
      const isDenied = await checkIfPermissionsDenied();
      setShowPermissionsBadge(isDenied);
    };

    checkPermissions();

    // Re-check when window gains focus
    window.addEventListener('focus', checkPermissions);
    return () => window.removeEventListener('focus', checkPermissions);
  }, []);

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
  const initializationStarted = useRef(false);

  useEffect(() => {
    const initializeFeed = async () => {
      if (!hasHydrated || initializationStarted.current) return;
      initializationStarted.current = true;

      try {
        // Send location first, then fetch feed
        const locationResult = await SendLocationWithRateLimit();
        if (locationResult === 'denied') {
          setIsLocationDenied(true);
          return;
        }
        setIsLocationDenied(false);

        const [status] = await Promise.all([
          fetchFeedStatus(),
          fetchFeed(),
          fetchUnseenNotificationCount(),
          fetchNpc(),
        ]);

        if (status.isInMeeting) {
          useFeedStore.setState({ isInMeeting: true });
          if (searchParams.get('auto-chat-redirect') !== 'false') {
            router.push(PAGE_ROUTES.CHAT);
            return;
          }
        }

        if (status.userIdToRate) {
          router.push(PAGE_ROUTES.RATE_USER(status.userIdToRate));
          return;
        }

        // If an NPC was fetched (or already exists), set a random index
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
  }, [
    hasHydrated,
    isInitialized,
    fetchFeedStatus,
    fetchFeed,
    fetchUnseenNotificationCount,
    fetchNpc,
    router,
  ]);

  const triggerReferralPrompt = () => {
    if (!showPermissionsBadge) {
      const hasSeenPrompt = localStorage.getItem(
        'has_seen_invite_friends_prompt',
      );
      if (!hasSeenPrompt) {
        setIsInvitePromptOpen(true);
      }
    }
  };

  const handleCloseInvitePrompt = () => {
    localStorage.setItem('has_seen_invite_friends_prompt', 'true');
    setIsInvitePromptOpen(false);
  };

  const handleGoToInvite = () => {
    localStorage.setItem('has_seen_invite_friends_prompt', 'true');
    setIsInvitePromptOpen(false);
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
      triggerReferralPrompt();
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
      if (!incomingExpiration) {
        // Only trigger referral prompt if we didn't receive their invite before
        // or this action will redirect to the chat almost immediately
        triggerReferralPrompt();
      }
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
      triggerReferralPrompt();
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
          <p className={styles.emptyStateText}>{tFeed('locationRequired')}</p>
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
          <p className={styles.emptyStateText}>{tFeed('noProfiles')}</p>
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
    if (isInMeeting || isLocationDenied || !currentItem) return null;

    if (currentItem.kind === 'npc') {
      return (
        <NpcFeedFooter
          onInvite={handleInvite}
          onDismiss={handleSkip}
          hasMonolog={!!monolog}
          isProcessing={isActionProcessing}
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
          isInviteDisabled={false}
          isProcessing={isActionProcessing}
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
          isOpen={isInvitePromptOpen}
          onClose={handleCloseInvitePrompt}
          message={tFeed('invitePrompt.message')}
          primaryButtonText={tFeed('invitePrompt.later')}
          onPrimaryAction={handleCloseInvitePrompt}
          secondaryButtonText={tFeed('invitePrompt.letsGo')}
          onSecondaryAction={handleGoToInvite}
        />
      </div>
    </div>
  );
}
