'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES } from '@/lib/routes';
import { useVenueStore } from '../use-venue-store';
import stylesCommon from '@/components/ui/common.module.css';
import styles from '../promotions/_components/promotion-form.module.css'; // Reusing Promotion Form styles for consistency

export default function AdvancedSettingsPage() {
  const t = useTranslations('screens.advancedSettings');
  const { id: venueId } = useParams() as { id: string };
  const { safeBack } = useSafeBack();
  const { currentVenue, fetchVenue, patchVenue, isLoading } = useVenueStore();

  const [qrCode, setQrCode] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (venueId) {
      fetchVenue(venueId);
    }
  }, [venueId, fetchVenue]);

  useEffect(() => {
    if (currentVenue) {
      setQrCode(currentVenue.qrCode || '');
    }
  }, [currentVenue]);

  const handleBack = () => {
    safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
  };

  const handleSave = async () => {
    if (!qrCode.trim()) return;

    setIsSubmitting(true);
    try {
      await patchVenue(venueId, { qrCode });
      handleBack();
    } catch (err: any) {
      toast.error(err.message || t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading || !currentVenue) {
    return (
      <div className="flex flex-1 items-center justify-center">
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
            onClick={handleBack}
          />
        }
      />
      <div className={styles.wrapper}>
        <div className={styles.form}>
          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{t('qrCode')}</FormSectionTitle>
            <Input
              value={qrCode}
              onChange={(e) => setQrCode(e.target.value)}
              placeholder={t('qrCodePlaceholder')}
              className={stylesCommon.inputDark}
            />
          </div>

          <div className={styles.footer}>
            <Button
              onClick={handleSave}
              loading={isSubmitting}
              className="w-[280px]"
              disabled={!qrCode.trim() || qrCode === currentVenue.qrCode}
            >
              {t('submit')}
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
