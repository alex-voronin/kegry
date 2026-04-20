import { useState, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { toast } from 'react-toastify';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { ApiResult } from '@/lib/api-types';
import { extractApiError } from '@/lib/api-error';
import { extractQrCode } from '@/lib/qr-utils';

interface UseQrCheckInOptions {
  /** Called after a successful check-in, e.g. to reload venue data. */
  onSuccess?: (venueId: string) => Promise<void> | void;
  /** Called when check-in fails or QR is invalid. */
  onError?: (venueId: string | null) => void;
}

interface CheckInResponse {
  venueId: string;
}

export function useQrCheckIn({ onSuccess, onError }: UseQrCheckInOptions = {}) {
  const t = useTranslations('components.qr-code-reader');
  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const [showCheckIn, setShowCheckIn] = useState(false);

  const handleQrCode = useCallback(
    async (rawValue: string) => {
      setIsScannerOpen(false);

      const qrCode = extractQrCode(rawValue, true) || '';
      if (!qrCode) {
        toast.error(t('toast.invalidQr'));
        onError?.(null);
        return;
      }

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

        if (response.data.success) {
          setShowCheckIn(true);
          await onSuccess?.(venueId);
        } else {
          const errorMessage = response.data?.errors?.[0]?.error;
          toast.error(errorMessage || t('toast.checkInFailed'));
          onError?.(venueId);
        }
      } catch (error: any) {
        const errorMessage = extractApiError(error)[0];
        toast.error(errorMessage || t('toast.checkInFailed'));
        onError?.(qrCode);
      }
    },
    [t, onSuccess, onError],
  );

  return {
    isScannerOpen,
    setIsScannerOpen,
    showCheckIn,
    setShowCheckIn,
    handleQrCode,
  };
}
