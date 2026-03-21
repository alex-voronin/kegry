'use client';

import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useVenueStore } from '../../../use-venue-store';
import { PAGE_ROUTES } from '@/lib/routes';
import { PromotionForm } from '../../_components/promotion-form';
import { toast } from 'react-toastify';
import styles from '../../_components/promotion-form.module.css';
import { PromoType } from '@/lib/promo-types';

export default function EditPromotionPage() {
  const t = useTranslations('screens.editPromotion');
  const params = useParams();
  const venueId = params.id as string;
  const promoId = params.promoId as string;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const { currentVenue, fetchVenue, updatePromotion } = useVenueStore();
  const { safeBack } = useSafeBack();

  useEffect(() => {
    if (!currentVenue || currentVenue.id !== venueId) {
      fetchVenue(venueId);
    }
  }, [venueId, currentVenue, fetchVenue]);

  const promo = currentVenue?.promos.find((p) => p.id === promoId);

  const onSubmit = async (values: { type: PromoType; body: string }) => {
    setIsSubmitting(true);
    try {
      await updatePromotion(venueId, promoId, values.type, values.body);
      safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
    } catch (err: any) {
      toast.error(err.message || t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!promo && !currentVenue) {
    return null;
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
        {promo && (
          <PromotionForm
            initialValues={{ type: promo.type, body: promo.body }}
            onSubmit={onSubmit}
            isSubmitting={isSubmitting}
            submitLabel={t('save')}
          />
        )}
      </div>
    </>
  );
}
