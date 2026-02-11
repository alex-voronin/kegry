'use client';

import { useEffect, useState } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { StarRating } from 'react-flexible-star-rating';
import styles from './feed.module.css';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { SegmentedControl } from '@/components/ui/segmented-control';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { PAGE_ROUTES } from '@/lib/routes';
import { useFeedStore, UserProfileDto } from '@/store/use-feed-store';
import { useSettingsStore, DistanceUnits } from '@/store/use-settings-store';
import { metersToKilometers, metersToMiles } from '@/lib/distance-utils';
import { toast } from 'react-toastify';
import { InviteExpiration, expiresAtToWhen } from '@/lib/invite-utils';
import { useFcmStore } from '@/lib/fcm/store';
import { NotificationType, FcmPayload } from '@/lib/fcm/types';
import { checkIfPermissionsDenied } from '@/lib/permissions';
import {
  SendLocationWithRateLimit,
  LocationResult,
} from '@/lib/use-background-location';

function FeedHeader({
  router,
  unseenCount,
  showPermissionsBadge,
}: {
  router: ReturnType<typeof useRouter>;
  unseenCount: number;
  showPermissionsBadge: boolean;
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
          <Icon
            src="/assets/feed/filters-icon.svg"
            alt="filters"
            width={32}
            height={32}
            priority
            onClick={() => router.push(PAGE_ROUTES.FEED_FILTERS)}
          />
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
        </div>
      }
    />
  );
}

function FeedBody({
  currentProfile,
  isLoading,
  distanceUnits,
  tFeed,
  tCommon,
  tHobbies,
}: {
  currentProfile: UserProfileDto | null;
  isLoading: boolean;
  distanceUnits: DistanceUnits;
  tFeed: ReturnType<typeof useTranslations>;
  tCommon: ReturnType<typeof useTranslations>;
  tHobbies: ReturnType<typeof useTranslations>;
}) {
  // Show spinner if loading and no cached content
  if (isLoading && !currentProfile) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner />
      </div>
    );
  }

  if (!currentProfile) {
    return (
      <div style={{ textAlign: 'center', marginTop: '50%' }}>
        No profiles available. Try changing the filter settings.
      </div>
    );
  }

  return (
    <>
      {isLoading && (
        <div className={styles.loadingOverlay}>
          <Spinner />
        </div>
      )}
      <div>
        <Image
          src={currentProfile.coverImageUrl}
          alt="profile"
          fill
          className={styles.profileImage}
          priority
        />
        <div className={styles.imageOverlay} />
      </div>

      <div className={styles.userInfo}>
        <div className={styles.ratingRow}>
          <StarRating
            initialRating={currentProfile.rating || 0}
            isReadOnly
            dimension={12}
            isHalfRatingEnabled={true}
          />
        </div>

        <h1 className={styles.name}>
          {currentProfile.firstName} {currentProfile.lastName}
        </h1>

        <div className={styles.location}>
          <Image
            src="/assets/feed/location-icon.svg"
            alt="location"
            width={20}
            height={31}
            priority
          />
          <div className={styles.locationText}>
            <span>
              {tFeed('distance', {
                distance:
                  distanceUnits === DistanceUnits.Miles
                    ? metersToMiles(currentProfile.distanceMeters)
                    : metersToKilometers(currentProfile.distanceMeters),
                units:
                  distanceUnits === DistanceUnits.Miles
                    ? tCommon('units.miles')
                    : tCommon('units.kilometers'),
              })}
            </span>
          </div>
        </div>

        <div className={styles.tags}>
          {currentProfile.hobbies.map((hobby) => (
            <span key={hobby.id} className={styles.tag}>
              {tHobbies(hobby.name)}
            </span>
          ))}
        </div>
      </div>
    </>
  );
}

function FeedFooter({
  onIgnore,
  onInvite,
  onSkip,
  isInviteDisabled,
}: {
  onIgnore: () => void;
  onInvite: () => void;
  onSkip: () => void;
  isInviteDisabled: boolean;
}) {
  return (
    <footer className={styles.actions}>
      <button
        className={styles.actionButton}
        onClick={(e) => {
          e.stopPropagation();
          onIgnore();
        }}
      >
        <Image
          className="pointer-events-none"
          src="/assets/feed/ignore-icon.svg"
          alt="ignore"
          width={29}
          height={29}
          priority
        />
      </button>
      <button
        className={styles.actionPrimary}
        onClick={(e) => {
          e.stopPropagation();
          onInvite();
        }}
        disabled={isInviteDisabled}
      >
        <Image
          className="pointer-events-none translate-x-[2px]"
          src="/assets/feed/invite-icon.webp"
          alt="invite"
          width={35}
          height={49}
          priority
        />
      </button>
      <button
        className={styles.actionButton}
        onClick={(e) => {
          e.stopPropagation();
          onSkip();
        }}
      >
        <Image
          className="pointer-events-none"
          src="/assets/feed/skip-icon.svg"
          alt="skip"
          width={24}
          height={24}
          priority
        />
      </button>
    </footer>
  );
}

function AvailableInvites({ count }: { count: number }) {
  if (count <= 0) return null;
  return (
    <div className={styles.availableInvites}>
      {Array.from({ length: count }).map((_, i) => (
        <Image
          key={i}
          src="/assets/feed/invite-icon.webp"
          alt="invite"
          width={28}
          height={28}
          className={styles.inviteCounter}
        />
      ))}
    </div>
  );
}

function WhenSelector({
  value,
  onChange,
  tFeed,
}: {
  value: InviteExpiration;
  onChange: (val: InviteExpiration) => void;
  tFeed: ReturnType<typeof useTranslations>;
}) {
  const timeOptions = [
    { value: InviteExpiration.Now, label: tFeed('now'), width: 50 },
    { value: InviteExpiration.Today, label: tFeed('today'), width: 60 },
    {
      value: InviteExpiration.Tomorrow,
      label: tFeed('tomorrow'),
      width: 90,
    },
  ];

  return (
    <div className={styles.whenSelector} onClick={(e) => e.stopPropagation()}>
      <SegmentedControl
        options={timeOptions}
        value={value}
        onChange={onChange as (val: string) => void}
        // hack, but bg-transparent class doesn't work in some versoins of Safari
        style={{ backgroundColor: 'transparent' }}
      />
    </div>
  );
}

export default function FeedScreen() {
  const tFeed = useTranslations('screens.feed');
  const tCommon = useTranslations('common');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const router = useRouter();
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
  } = useFeedStore();
  const [showPermissionsBadge, setShowPermissionsBadge] = useState(false);
  const [isLocationDenied, setIsLocationDenied] = useState(false);
  const [when, setWhen] = useState<InviteExpiration>(InviteExpiration.Today);
  const [expirationPopup, setExpirationPopup] = useState<{
    isOpen: boolean;
    suggestedWhen: InviteExpiration;
  }>({ isOpen: false, suggestedWhen: InviteExpiration.Today });

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

  const currentProfile = feed[0] || null;

  const [isInitialized, setIsInitialized] = useState(false);

  useEffect(() => {
    const initializeFeed = async () => {
      if (!hasHydrated) return;
      if (isInitialized) return;

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
        ]);

        if (status.isInMeeting) {
          router.push(PAGE_ROUTES.CHAT);
          return;
        }

        if (status.userIdToRate) {
          router.push(PAGE_ROUTES.RATE_USER(status.userIdToRate));
          return;
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
    router,
  ]);

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

  const handleIgnore = async () => {
    if (!currentProfile) return;
    try {
      await ignoreUser(currentProfile.id);
    } catch (err: any) {
      toast.error(err.message);
    }
  };

  const handleInvite = async () => {
    if (!currentProfile) return;
    try {
      const incomingExpiration = await getInviteExpiration(currentProfile.id);
      if (incomingExpiration) {
        const theirWhen = expiresAtToWhen(incomingExpiration);
        if (theirWhen && theirWhen !== when) {
          setExpirationPopup({ isOpen: true, suggestedWhen: theirWhen });
          return;
        }
      }
      await inviteUser(currentProfile.id, when);
    } catch (err: any) {
      toast.error(err.message);
    }
  };

  const handleAcceptSuggestedExpiration = async () => {
    if (!currentProfile) return;
    try {
      await inviteUser(currentProfile.id, expirationPopup.suggestedWhen);
    } catch (err: any) {
      toast.error(err.message);
    }
  };

  const handleSkip = async () => {
    if (!currentProfile) return;
    try {
      await skipUser(currentProfile.id);
    } catch (err: any) {
      toast.error(err.message);
    }
  };

  const handleProfileClick = () => {
    if (!currentProfile) return;
    router.push(PAGE_ROUTES.USER_PROFILE(currentProfile.id));
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
          />
        </div>
        {showLoading && !currentProfile ? (
          <div className={styles.loadingContainer}>
            <Spinner />
          </div>
        ) : isLocationDenied ? (
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
        ) : currentProfile ? (
          <FeedBody
            currentProfile={currentProfile}
            isLoading={showLoading}
            distanceUnits={distanceUnits}
            tFeed={tFeed}
            tCommon={tCommon}
            tHobbies={tHobbies}
          />
        ) : (
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
        )}
        <WhenSelector value={when} onChange={setWhen} tFeed={tFeed} />
        <AvailableInvites count={availableInvitesCount} />
        <FeedFooter
          onIgnore={handleIgnore}
          onInvite={handleInvite}
          onSkip={handleSkip}
          isInviteDisabled={availableInvitesCount <= 0}
        />
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
      </div>
    </div>
  );
}
