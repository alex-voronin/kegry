'use client';

import { useEffect } from 'react';
import { useParams } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { useVenueDiscountsStore } from './use-venue-discounts-store';
import styles from './venue-discounts.module.css';

const daysOfWeek = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
] as const;

export default function VenueDiscountsPage() {
  const router = useRouter();
  const params = useParams();
  const venueId = params.id as string;

  const tCommon = useTranslations('common');
  const t = useTranslations('screens.venueDetail');

  const { discounts, isLoading, fetchDiscounts, reset } =
    useVenueDiscountsStore();

  useEffect(() => {
    if (venueId) {
      fetchDiscounts(venueId).catch(() => {
        toast.error('Failed to load discounts');
      });
    }
    return () => {
      reset();
    };
  }, [venueId, fetchDiscounts, reset]);

  const handleBack = () => {
    router.back();
  };

  if (isLoading || !discounts) {
    return (
      <div className="flex h-screen flex-1 flex-col">
        <Header
          title={t('myDiscount')}
          left={
            <Icon
              src="/assets/common/disclosure-left-icon.svg"
              alt="back"
              width={14}
              height={27}
              onClick={handleBack}
            />
          }
        />
        <div className="flex flex-1 items-center justify-center">
          <Spinner />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.wrapper}>
      <Header
        title={t('myDiscount')}
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={handleBack}
          />
        }
      />

      <div className={styles.content}>
        <div className={styles.section}>
          {daysOfWeek.map((day) => {
            const dayKey = day.toLowerCase() as keyof typeof discounts;
            const discountData = discounts[dayKey];
            const currentDiscount = discountData?.currentLevelDiscount ?? 0;
            const nextDiscount = discountData?.nextLevelDiscount;

            return (
              <div key={day} className={styles.field}>
                <div className={styles.loyaltyHeader}>
                  <span className={styles.dayTitle}>
                    {tCommon(`days.${day}`)}
                  </span>
                  <span className={styles.loyaltyDiscount}>
                    {t('discount', { percent: currentDiscount })}
                  </span>
                </div>

                <div className={styles.progressText}>
                  {nextDiscount != null
                    ? t('nextDiscountLevelInfo', { percent: nextDiscount })
                    : t('maxDiscountLevelReached')}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
