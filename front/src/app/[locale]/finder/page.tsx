'use client';

import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { StarRating } from 'react-flexible-star-rating';
import styles from './finder.module.css';
import { Icon } from '@/components/ui/icon';

export default function FinderScreen() {
  const t = useTranslations('screens.finder');

  return (
    <div className={styles.wrapper}>
      <div className={styles.content}>
        <header className={styles.header}>
          <Icon
            src="/assets/finder/profile-icon.svg"
            alt="profile"
            width={30}
            height={32}
            priority
          />
          <div className={styles.headerRightSideIconsWrapper}>
            <Icon
              src="/assets/finder/filters-icon.svg"
              alt="filters"
              width={32}
              height={32}
              priority
            />
            <Icon
              src="/assets/finder/notifications-icon.svg"
              alt="notifications"
              width={28}
              height={30}
              priority
            >
              <span className={styles.badge}>3</span>
            </Icon>
          </div>
        </header>

        <div>
          <Image
            src="/assets/finder/profile-image.png"
            alt="profile"
            fill
            className={styles.profileImage}
            priority
          />
          <div className={styles.imageOverlay} />
        </div>

        <div className={styles.userInfo}>
          <div className={styles.ratingRow}>
            <StarRating initialRating={4} isReadOnly dimension={20} />
          </div>

          <h1 className={styles.name}>Ivan Laptev</h1>

          <div className={styles.location}>
            <Image
              src="/assets/finder/location-icon.svg"
              alt="location"
              width={32}
              height={32}
              priority
            />
            <span>{t('distance', { km: 3 })}</span>
          </div>

          <div className={styles.tags}>
            <span className={styles.tag}>🏐 Volleyball</span>
            <span className={styles.tag}>🥊 Boxing</span>
            <span className={styles.tag}>🏕 Camping</span>
          </div>
        </div>

        <footer className={styles.actions}>
          <button className={styles.actionButton}>
            <Image
              className="pointer-events-none"
              src="/assets/finder/ignore-icon.svg"
              alt="ignore"
              width={29}
              height={29}
              priority
            />
          </button>
          <button className={styles.actionPrimary}>
            <Image
              className="pointer-events-none"
              src="/assets/finder/invite-icon.png"
              alt="invite"
              width={40}
              height={42}
              priority
            />
          </button>
          <button className={styles.actionButton}>
            <Image
              className="pointer-events-none"
              src="/assets/finder/temporary-block-icon.svg"
              alt="block"
              width={24}
              height={24}
              priority
            />
          </button>
        </footer>
      </div>
    </div>
  );
}
