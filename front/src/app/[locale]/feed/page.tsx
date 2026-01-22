'use client';

import { useEffect, useRef } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { StarRating } from 'react-flexible-star-rating';
import styles from './feed.module.css';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { PAGE_ROUTES } from '@/lib/routes';
import { useFeedStore, UserProfileDto } from '@/store/use-feed-store';
import { useNotificationStore } from '@/store/use-notification-store';
import { toast } from 'react-toastify';

function FeedHeader({
  router,
  unseenCount,
}: {
  router: ReturnType<typeof useRouter>;
  unseenCount: number;
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
        />
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
  tFeed,
  tHobbies,
}: {
  currentProfile: UserProfileDto | null;
  isLoading: boolean;
  tFeed: ReturnType<typeof useTranslations>;
  tHobbies: ReturnType<typeof useTranslations>;
}) {
  if (isLoading) {
    return (
      <div style={{ textAlign: 'center', marginTop: '50%' }}>Loading...</div>
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
          />
        </div>

        <h1 className={styles.name}>
          {currentProfile.firstName} {currentProfile.lastName}
        </h1>

        <div className={styles.location}>
          <Image
            src="/assets/feed/location-icon.svg"
            alt="location"
            width={24}
            height={38}
            priority
          />
          <div className={styles.locationText}>
            <span>
              {tFeed('availabilityTime', {
                time: 'today',
              })}
            </span>
            <span>
              {tFeed('distance', {
                km: Math.round(currentProfile.distanceMeters / 1000),
                units: 'km',
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
}: {
  onIgnore: () => void;
  onInvite: () => void;
  onSkip: () => void;
}) {
  return (
    <footer className={styles.actions}>
      <button className={styles.actionButton} onClick={onIgnore}>
        <Image
          className="pointer-events-none"
          src="/assets/feed/ignore-icon.svg"
          alt="ignore"
          width={29}
          height={29}
          priority
        />
      </button>
      <button className={styles.actionPrimary} onClick={onInvite}>
        <Image
          className="pointer-events-none"
          src="/assets/feed/invite-icon.png"
          alt="invite"
          width={40}
          height={42}
          priority
        />
      </button>
      <button className={styles.actionButton} onClick={onSkip}>
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

export default function FeedScreen() {
  const tFeed = useTranslations('screens.feed');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const router = useRouter();
  const {
    feed,
    fetchFeedStatus,
    fetchFeed,
    inviteUser,
    skipUser,
    ignoreUser,
    isLoading,
    hasHydrated,
  } = useFeedStore();
  const { unseenCount, fetchUnseenCount } = useNotificationStore();

  const currentProfile = feed[0] || null;

  const isInitialized = useRef(false);

  useEffect(() => {
    const initializeFeed = async () => {
      if (!hasHydrated) return;
      if (isInitialized.current) return;
      isInitialized.current = true;

      try {
        const [status] = await Promise.all([
          fetchFeedStatus(),
          fetchFeed(),
          fetchUnseenCount(),
        ]);

        if (status.isInMeeting) {
          router.push(PAGE_ROUTES.CHAT);
          return;
        }

        if (status.userIdToRate) {
          console.log('User to rate:', status.userIdToRate);
        }
      } catch (err: any) {
        toast.error(err.message);
        isInitialized.current = false;
      }
    };

    initializeFeed();
  }, [hasHydrated, fetchFeedStatus, fetchFeed, fetchUnseenCount, router]);

  const showLoading = isLoading || !hasHydrated;

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
      await inviteUser(currentProfile.id);
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

  return (
    <div className={styles.wrapper}>
      <div className={styles.content}>
        <FeedHeader router={router} unseenCount={unseenCount} />
        <FeedBody
          currentProfile={currentProfile}
          isLoading={showLoading}
          tFeed={tFeed}
          tHobbies={tHobbies}
        />
        <FeedFooter
          onIgnore={handleIgnore}
          onInvite={handleInvite}
          onSkip={handleSkip}
        />
      </div>
    </div>
  );
}
