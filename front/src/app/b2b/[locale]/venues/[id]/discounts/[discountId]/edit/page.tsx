'use client';

import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useVenueStore } from '../../../use-venue-store';
import { PAGE_ROUTES } from '@/lib/routes';
import { DiscountForm } from '../../_components/discount-form';
import { toast } from 'react-toastify';
import styles from '../../_components/discount-form.module.css';
import { Spinner } from '@/components/ui/spinner';

export default function EditDiscountPage() {
  const t = useTranslations('screens.editDiscount');
  const params = useParams();
  const venueId = params.id as string;
  const discountId = params.discountId as string;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const { currentVenue, fetchVenue, updateDiscount, isLoading } = useVenueStore();
  const { safeBack } = useSafeBack();

  useEffect(() => {
    if (venueId && currentVenue?.id !== venueId) {
      fetchVenue(venueId);
    }
  }, [venueId, currentVenue?.id, fetchVenue]);

  const discount = currentVenue?.discounts.find((d) => d.id === discountId);

  const onSubmit = async (values: {
    percent: number;
    days: string[];
    loyaltyLevelId: number | null;
  }) => {
    setIsSubmitting(true);
    try {
      await updateDiscount(
        venueId,
        discountId,
        values.percent,
        values.days,
        values.loyaltyLevelId,
      );
      safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
    } catch (err: any) {
      toast.error(err.message || t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading || !currentVenue || !discount) {
    return (
      <div className="flex flex-1 items-center justify-center h-screen bg-background">
        <Spinner />
      </div>
    );
  }

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
          initialValues={{
            percent: discount.percent,
            days: discount.days,
            loyaltyLevelId: discount.loyaltyLevelId,
          }}
          loyaltyLevels={currentVenue.loyaltyLevels}
          onSubmit={onSubmit}
          isSubmitting={isSubmitting}
          submitLabel={t('save')}
          translationNamespace="editDiscount"
        />
      </div>
    </>
  );
}
