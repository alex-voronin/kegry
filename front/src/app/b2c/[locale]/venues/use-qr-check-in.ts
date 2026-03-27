import { useState, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { toast } from 'react-toastify';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { ApiResult } from '@/lib/api-types';

import { isOurDomain } from '@/lib/qr-utils';

interface UseQrCheckInOptions {
  /** Called after a successful check-in, e.g. to reload venue data. */
  onSuccess?: (venueId: string) => Promise<void> | void;
}

interface CheckInResponse {
  venueId: string;
}

export function useQrCheckIn({ onSuccess }: UseQrCheckInOptions = {}) {
  const t = useTranslations('components.qr-code-reader');
  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const [showCheckIn, setShowCheckIn] = useState(false);

  const handleQrCode = useCallback(
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
          const position = await new Promise<GeolocationPosition>(
            (resolve, reject) => {
              navigator.geolocation.getCurrentPosition(resolve, reject);
            },
          );

          const response = await apiClient.post<ApiResult<CheckInResponse>>(
            API_ENDPOINTS.VENUES.QR_CHECK_IN(qrCode),
            {
              latitude: position.coords.latitude,
              longitude: position.coords.longitude,
            },
          );

          const { venueId } = response.data.result;

          setShowCheckIn(true);
          await onSuccess?.(venueId);
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
    handleQrCode,
  };
}
