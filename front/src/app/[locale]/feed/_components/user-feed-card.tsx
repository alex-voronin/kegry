import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { StarRating } from 'react-flexible-star-rating';
import styles from '../feed.module.css';
import { Spinner } from '@/components/ui/spinner';
import { SegmentedControl } from '@/components/ui/segmented-control';
import { UserProfileDto } from '../use-feed-store';
import { DistanceUnits } from '@/app/[locale]/settings/use-settings-store';
import { metersToKilometers, metersToMiles } from '@/lib/distance-utils';
import { InviteExpiration } from '@/lib/invite-utils';
import { cn } from '@/lib/utils';

export function UserFeedBody({
  currentProfile,
  isLoading,
  distanceUnits,
  tFeed,
  tCommon,
  tHobbies,
}: {
  currentProfile: UserProfileDto;
  isLoading: boolean;
  distanceUnits: DistanceUnits;
  tFeed: ReturnType<typeof useTranslations>;
  tCommon: ReturnType<typeof useTranslations>;
  tHobbies: ReturnType<typeof useTranslations>;
}) {
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
        {currentProfile.rating && currentProfile.rating > Number.EPSILON && (
          <div className={styles.ratingRow}>
            <StarRating
              initialRating={currentProfile.rating || 0}
              isReadOnly
              dimension={12}
              isHalfRatingEnabled={true}
            />
          </div>
        )}

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

export function UserFeedFooter({
  onIgnore,
  onInvite,
  onSkip,
  isInviteDisabled,
  isPerformingAction,
}: {
  onIgnore: () => void;
  onInvite: () => void;
  onSkip: () => void;
  isInviteDisabled: boolean;
  isPerformingAction: boolean;
}) {
  return (
    <footer
      className={cn(
        styles.actions,
        isPerformingAction && styles.isPerformingAction,
      )}
    >
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

export function AvailableInvites({ count }: { count: number }) {
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

export function WhenSelector({
  value,
  onChange,
  tFeed,
  className,
  itemWidth,
  fullWidth,
  stretchItems,
}: {
  value: InviteExpiration;
  onChange: (val: InviteExpiration) => void;
  tFeed: ReturnType<typeof useTranslations>;
  className?: string;
  itemWidth?: string | number;
  fullWidth?: boolean;
  stretchItems?: boolean;
}) {
  const timeOptions = [
    {
      value: InviteExpiration.Now,
      label: tFeed('now'),
      width: itemWidth ?? 50,
    },
    {
      value: InviteExpiration.Today,
      label: tFeed('today'),
      width: itemWidth ?? 60,
    },
    {
      value: InviteExpiration.Tomorrow,
      label: tFeed('tomorrow'),
      width: itemWidth ?? 90,
    },
  ];

  return (
    <div
      className={className ?? styles.whenSelector}
      onClick={(e) => e.stopPropagation()}
    >
      <SegmentedControl
        options={timeOptions}
        value={value}
        onChange={onChange as (val: string) => void}
        // hack, but bg-transparent class doesn't work in some versions of Safari
        style={{ backgroundColor: 'transparent' }}
        className={fullWidth ? 'w-full' : undefined}
        stretchItems={stretchItems}
      />
    </div>
  );
}
