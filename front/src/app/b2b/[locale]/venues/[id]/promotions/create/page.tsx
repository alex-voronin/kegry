'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useVenueStore } from '../../use-venue-store';
import { PAGE_ROUTES } from '@/lib/routes';
import { PromotionForm } from '../_components/promotion-form';
import { toast } from 'react-toastify';
import styles from '../_components/promotion-form.module.css';

export default function AddPromotionPage() {
  const t = useTranslations('screens.createPromotion');
  const params = useParams();
  const venueId = params.id as string;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const { createPromotion } = useVenueStore();
  const { safeBack } = useSafeBack();

  const onSubmit = async (values: { body: string }) => {
    setIsSubmitting(true);
    try {
      await createPromotion(venueId, values.body);
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
        <PromotionForm
          onSubmit={onSubmit}
          isSubmitting={isSubmitting}
          submitLabel={t('save')}
          translationNamespace="createPromotion"
        />
      </div>
    </>
  );
}
