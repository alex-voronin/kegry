'use client';

import { useEffect } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { StarRating } from 'react-flexible-star-rating';
import styles from './feed.module.css';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { PAGE_ROUTES } from '@/lib/routes';
import { useFeedStore, UserProfileDto } from '@/store/use-feed-store';
import { toast } from 'react-toastify';

function FeedHeader({ router }: { router: ReturnType<typeof useRouter> }) {
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
            <span className={styles.badge}>3</span>
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

function FeedFooter({ router }: { router: ReturnType<typeof useRouter> }) {
  return (
    <footer className={styles.actions}>
      <button className={styles.actionButton}>
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
        onClick={() => router.push(PAGE_ROUTES.CHAT)}
      >
        <Image
          className="pointer-events-none"
          src="/assets/feed/invite-icon.png"
          alt="invite"
          width={40}
          height={42}
          priority
        />
      </button>
      <button className={styles.actionButton}>
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
    currentProfileIndex,
    fetchFeedStatus,
    fetchFeed,
    isLoading,
    error,
    hasHydrated,
  } = useFeedStore();

  useEffect(() => {
    const initializeFeed = async () => {
      if (!hasHydrated) return;

      try {
        const [status] = await Promise.all([fetchFeedStatus(), fetchFeed()]);

        if (status.isInMeeting) {
          router.push(PAGE_ROUTES.CHAT);
          return;
        }

        if (status.userIdToRate) {
          console.log('User to rate:', status.userIdToRate);
        }
      } catch (err: any) {
        toast.error(err.message);
      }
    };

    initializeFeed();
  }, [hasHydrated, fetchFeedStatus, fetchFeed, router]);

  useEffect(() => {
    if (error) {
      toast.error(error);
    }
  }, [error]);

  const currentProfile = feed[currentProfileIndex] || null;
  const showLoading = isLoading || !hasHydrated;

  return (
    <div className={styles.wrapper}>
      <div className={styles.content}>
        <FeedHeader router={router} />
        <FeedBody
          currentProfile={currentProfile}
          isLoading={showLoading}
          tFeed={tFeed}
          tHobbies={tHobbies}
        />
        <FeedFooter router={router} />
      </div>
    </div>
  );
}
