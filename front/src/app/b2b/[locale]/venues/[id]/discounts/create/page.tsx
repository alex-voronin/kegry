'use client';

import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useVenueStore } from '../../use-venue-store';
import { PAGE_ROUTES } from '@/lib/routes';
import { DiscountForm } from '../_components/discount-form';
import { toast } from 'react-toastify';
import styles from '../_components/discount-form.module.css';

export default function CreateDiscountPage() {
  const t = useTranslations('screens.createDiscount');
  const params = useParams();
  const venueId = params.id as string;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const { fetchVenue, createDiscount } = useVenueStore();
  const { safeBack } = useSafeBack();

  useEffect(() => {
    if (venueId) {
      fetchVenue(venueId);
    }
  }, [venueId, fetchVenue]);

  const onSubmit = async (values: { percent: number; days: string[] }) => {
    setIsSubmitting(true);
    try {
      await createDiscount(venueId, values.percent, values.days);
      safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
    } catch (err: any) {
      toast.error(err.message || t('toast.createFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <Header
        title={t('title')}
        left={
          <Icon
            src="/assets/b2b/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId))}
          />
        }
      />
      <div className={styles.wrapper}>
        <DiscountForm
          onSubmit={onSubmit}
          isSubmitting={isSubmitting}
          submitLabel={t('save')}
          translationNamespace="createDiscount"
        />
      </div>
    </>
  );
}
