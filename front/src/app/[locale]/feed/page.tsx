'use client';

import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { StarRating } from 'react-flexible-star-rating';
import styles from './feed.module.css';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { PAGE_ROUTES } from '@/lib/routes';

export default function FeedScreen() {
  const t = useTranslations('screens.feed');
  const router = useRouter();

  return (
    <div className={styles.wrapper}>
      <div className={styles.content}>
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

        <div>
          <Image
            src="/assets/feed/profile-image.png"
            alt="profile"
            fill
            className={styles.profileImage}
            priority
          />
          <div className={styles.imageOverlay} />
        </div>

        <div className={styles.userInfo}>
          <div className={styles.ratingRow}>
            <StarRating initialRating={4} isReadOnly dimension={12} />
          </div>

          <h1 className={styles.name}>Ivan Laptev</h1>

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
                {t('availabilityTime', {
                  time: 'today',
                })}
              </span>
              <span>
                {t('distance', {
                  km: 3,
                  units: 'km',
                })}
              </span>
            </div>
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
              src="/assets/feed/temporary-block-icon.svg"
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
