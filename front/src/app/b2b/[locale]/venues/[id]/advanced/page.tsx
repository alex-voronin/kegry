'use client';

import { useState, useEffect, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { Toggle } from '@/components/ui/toggle';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES } from '@/lib/routes';
import { buildQrUrl, downloadQrCode, extractQrCode } from '@/lib/qr-utils';
import { useVenueStore } from '../use-venue-store';
import { Download, ScanQrCode } from 'lucide-react';
import { cn } from '@/lib/utils';
import { QrScannerPopup } from '@/components/common/qr-scanner-popup';
import stylesCommon from '@/components/ui/common.module.css';
import styles from '../promotions/_components/promotion-form.module.css'; // Reusing Promotion Form styles for consistency

export default function AdvancedSettingsPage() {
  const t = useTranslations('screens.advancedSettings');
  const tCommon = useTranslations('common');
  const { id: venueId } = useParams() as { id: string };
  const { safeBack } = useSafeBack();
  const { currentVenue, fetchVenue, patchVenue, isLoading } = useVenueStore();

  const [qrCode, setQrCode] = useState('');
  const [pinCode, setPinCode] = useState('');
  const [isGeneratedPinCode, setIsGeneratedPinCode] = useState(false);
  const [isPinCheckInAllowed, setIsPinCheckInAllowed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDownloadMenuOpen, setIsDownloadMenuOpen] = useState(false);
  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const [showSaveDialog, setShowSaveDialog] = useState(false);

  useEffect(() => {
    if (venueId) {
      fetchVenue(venueId);
    }
  }, [venueId, fetchVenue]);

  useEffect(() => {
    if (currentVenue) {
      setQrCode(currentVenue.qrCode || '');
      setPinCode(currentVenue.pinCode || '');
      setIsGeneratedPinCode(currentVenue.isGeneratedPinCode);
      setIsPinCheckInAllowed(currentVenue.isPinCheckInAllowed);
    }
  }, [currentVenue]);

  const goBack = useCallback(() => {
    safeBack(PAGE_ROUTES.B2B.VENUES_DETAIL(venueId));
  }, [safeBack, venueId]);

  const isDirty = currentVenue
    ? qrCode !== (currentVenue.qrCode || '') ||
      isGeneratedPinCode !== currentVenue.isGeneratedPinCode ||
      isPinCheckInAllowed !== currentVenue.isPinCheckInAllowed ||
      (!isGeneratedPinCode && pinCode !== (currentVenue.pinCode || ''))
    : false;

  const handleBack = () => {
    if (isDirty) {
      setShowSaveDialog(true);
    } else {
      goBack();
    }
  };

  const handleSave = async () => {
    if (!qrCode.trim()) return;

    setIsSubmitting(true);
    try {
      await patchVenue(venueId, {
        qrCode,
        pinCode: isGeneratedPinCode ? '' : pinCode,
        isPinCheckInAllowed,
      });
      goBack();
    } catch (err: any) {
      toast.error(err.message || t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDownloadHighContrastQr = () => {
    downloadQrCode(buildQrUrl(qrCode), 'bw');
  };

  if (isLoading || !currentVenue) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  const isDownloadEnabled =
    qrCode === currentVenue.qrCode && qrCode.trim().length > 0;

  const handleScan = (scannedValue: string) => {
    const qrCode = extractQrCode(scannedValue, false) || '';
    setQrCode(qrCode);
    setIsScannerOpen(false);
  };

  const downloadOptions = [
    {
      icon: <Download size={24} className="text-primary" />,
      label: t('downloadDefaultQr'),
      onClick: () => {
        setIsDownloadMenuOpen(false);
        downloadQrCode(buildQrUrl(qrCode), 'default');
      },
    },
    {
      icon: <Download size={24} className="text-primary" />,
      label: t('downloadHighContrastQr'),
      onClick: () => {
        setIsDownloadMenuOpen(false);
        handleDownloadHighContrastQr();
      },
    },
  ];

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
        right={
          <Icon
            onClick={() => setIsScannerOpen(true)}
            className="cursor-pointer"
          >
            <ScanQrCode size={24} className="text-primary" />
          </Icon>
        }
      />
      <div className={styles.wrapper}>
        <div className={styles.form}>
          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{t('qrCode')}</FormSectionTitle>
            <div className="flex items-center gap-2">
              <Input
                value={qrCode}
                onChange={(e) => setQrCode(e.target.value)}
                placeholder={t('qrCodePlaceholder')}
                className={cn(stylesCommon.inputDark, 'flex-1')}
              />
              <button
                type="button"
                className={cn(
                  'flex h-12 w-12 shrink-0 items-center justify-center rounded-xl transition-all',
                  isDownloadEnabled
                    ? 'bg-surface-lighter text-primary hover:bg-surface-light active:scale-95'
                    : 'bg-surface-dark text-secondary cursor-not-allowed opacity-40',
                )}
                onClick={() => setIsDownloadMenuOpen(true)}
                disabled={!isDownloadEnabled}
                title={t('downloadHighContrastQr')}
              >
                <Download size={24} />
              </button>
            </div>
          </div>

          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{t('pinCode')}</FormSectionTitle>
            <div className="flex flex-col gap-4">
              <Input
                value={isGeneratedPinCode ? currentVenue.pinCode : pinCode}
                onChange={(e) => setPinCode(e.target.value)}
                placeholder={t('pinCodePlaceholder')}
                className={cn(stylesCommon.inputDark, 'w-full')}
                disabled={isGeneratedPinCode}
                maxLength={4}
              />
              <div className="flex items-center justify-between">
                <span className="text-secondary text-sm">
                  {t('allowPinCheckIn')}
                </span>
                <Toggle
                  checked={isPinCheckInAllowed}
                  onChange={setIsPinCheckInAllowed}
                />
              </div>
              <div className="flex items-center justify-between">
                <span className="text-secondary text-sm">
                  {t('generatePinEveryday')}
                </span>
                <Toggle
                  checked={isGeneratedPinCode}
                  onChange={setIsGeneratedPinCode}
                />
              </div>
            </div>
          </div>

          <div className={styles.footer}>
            <Button
              onClick={handleSave}
              loading={isSubmitting}
              className="w-[280px]"
              disabled={!qrCode.trim() || !isDirty}
            >
              {t('submit')}
            </Button>
          </div>
        </div>
      </div>

      <ModalMenu
        isOpen={isDownloadMenuOpen}
        onClose={() => setIsDownloadMenuOpen(false)}
        options={downloadOptions}
      />

      <QrScannerPopup
        isOpen={isScannerOpen}
        onClose={() => setIsScannerOpen(false)}
        onScan={handleScan}
        checkLocation={false}
      />

      <ModalDialog
        isOpen={showSaveDialog}
        onClose={() => setShowSaveDialog(false)}
        message={tCommon('saveChangesDialog.message')}
        primaryButtonText={tCommon('saveChangesDialog.no')}
        secondaryButtonText={tCommon('saveChangesDialog.yes')}
        onPrimaryAction={goBack}
        onSecondaryAction={handleSave}
      />
    </>
  );
}
