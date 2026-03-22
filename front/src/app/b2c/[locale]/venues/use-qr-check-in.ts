import { useState, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { toast } from 'react-toastify';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';

function isOurDomain(url: URL): boolean {
  const { hostname } = window.location;
  const ownRoot = hostname.split('.').slice(-2).join('.');
  const scannedRoot = url.hostname.split('.').slice(-2).join('.');
  return ownRoot === scannedRoot;
}

interface UseQrCheckInOptions {
  /** Called after a successful check-in, e.g. to reload venue data. */
  onSuccess?: () => Promise<void> | void;
  /** Translation namespace to look up toast.invalidQr / toast.checkInFailed */
  translationNs: 'screens.venues' | 'screens.venueDetail';
}

export function useQrCheckIn({ onSuccess, translationNs }: UseQrCheckInOptions) {
  const t = useTranslations(translationNs);
  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const [showCheckIn, setShowCheckIn] = useState(false);

  const handleScan = useCallback(
    async (rawValue: string) => {
      setIsScannerOpen(false);

      let url: URL;
      try {
        url = new URL(rawValue);
      } catch {
        toast.error(t('toast.invalidQr'));
        return;
      }

      const qrCode = url.searchParams.get('v');

      if (qrCode && isOurDomain(url)) {
        try {
          const position = await new Promise<GeolocationPosition>((resolve, reject) => {
            navigator.geolocation.getCurrentPosition(resolve, reject);
          });

          await apiClient.post(API_ENDPOINTS.VENUE_QR_CHECK_IN(qrCode), {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
          });

          setShowCheckIn(true);
          await onSuccess?.();
        } catch {
          toast.error(t('toast.checkInFailed'));
        }
      } else {
        toast.error(t('toast.invalidQr'));
      }
    },
    [t, onSuccess],
  );

  return {
    isScannerOpen,
    setIsScannerOpen,
    showCheckIn,
    setShowCheckIn,
    handleScan,
  };
}
