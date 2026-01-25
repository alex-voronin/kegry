'use client';

import { useEffect, useState, useRef } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { StarRating } from 'react-flexible-star-rating';
import styles from './rate-user.module.css';
import { PAGE_ROUTES } from '@/lib/routes';
import { useFeedStore } from '@/store/use-feed-store';
import { useRateUserStore } from '@/store/use-rate-user-store';
import { toast } from 'react-toastify';
import { Spinner } from '@/components/ui/spinner';
import { Button, ButtonVariant } from '@/components/ui/button';

export default function RateUserScreen() {
  const t = useTranslations('screens.rateUser');
  const router = useRouter();
  const { fetchFeedStatus } = useFeedStore();
  const {
    userProfile,
    fetchUserToRate,
    submitRating,
    isLoading: isProfileLoading,
    isSubmitting,
  } = useRateUserStore();

  const [rating, setRating] = useState(0);
  const [initialLoading, setInitialLoading] = useState(true);
  const initialized = useRef(false);

  useEffect(() => {
    if (initialized.current) return;
    initialized.current = true;

    const init = async () => {
      try {
        const status = await fetchFeedStatus();

        if (status.userIdToRate) {
          await fetchUserToRate(status.userIdToRate);
        } else {
          console.log('No user to rate found in status, redirecting to feed.');
          router.replace(PAGE_ROUTES.FEED);
          return;
        }
      } catch (error) {
        console.error('Error in RateUser init:', error);
        router.replace(PAGE_ROUTES.FEED);
      } finally {
        setInitialLoading(false);
      }
    };

    init();
  }, [fetchFeedStatus, fetchUserToRate, router]);

  const handleSubmit = async () => {
    if (!userProfile) {
      console.log('No user to rate found, redirecting to feed.');
      router.replace(PAGE_ROUTES.FEED);
      return;
    }
    if (rating === 0) {
      toast.error(t('toast.ratingRequired'));
      return;
    }

    try {
      await submitRating(userProfile.id, rating);
      toast.success(t('toast.ratingSubmitted'));
      router.replace(PAGE_ROUTES.FEED);
    } catch (error: any) {
      toast.error(error.message || t('toast.submitFailed'));
    }
  };

  const handleSkip = () => {
    router.replace(PAGE_ROUTES.FEED);
  };

  if (initialLoading || isProfileLoading) {
    return (
      <div className="flex h-dvh items-center justify-center">
        <Spinner />
      </div>
    );
  }

  return (
    <div className={styles.wrapper}>
      <div className={styles.content}>
        <Image
          src={
            userProfile?.coverImageUrl ||
            '/assets/common/avatar-placeholder.svg'
          }
          alt="cover"
          fill
          className={styles.profileImage}
          priority
        />
        <div className={styles.imageOverlay} />

        <div className={styles.userInfo}>
          <h1 className={styles.name}>
            {userProfile?.firstName} {userProfile?.lastName}
          </h1>
        </div>

        <div className={styles.footerModal}>
          <h2 className={styles.modalTitle}>{t('howWasIt')}</h2>

          <div className={styles.starRatingWrapper}>
            <StarRating
              initialRating={rating}
              onRatingChange={(r: number) => setRating(r)}
              dimension={18}
              isReadOnly={isSubmitting}
              isHalfRatingEnabled={true}
            />
          </div>

          <div className={styles.buttonsRow}>
            <Button
              variant={ButtonVariant.Default}
              className={styles.button}
              onClick={handleSubmit}
              disabled={isSubmitting}
            >
              {isSubmitting ? <Spinner /> : t('submit')}
            </Button>
            <Button
              variant={ButtonVariant.Default}
              className={styles.button}
              onClick={handleSkip}
              disabled={isSubmitting}
            >
              {t('skip')}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
