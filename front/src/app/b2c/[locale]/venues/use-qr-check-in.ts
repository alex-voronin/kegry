import { useState, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { toast } from 'react-toastify';
import apiClient from '@/lib/api-client';
import { API_ENDPOINTS } from '@/lib/routes';
import { ApiResult } from '@/lib/api-types';
import { extractApiError } from '@/lib/api-error';
import { extractQrCode } from '@/lib/qr-utils';
import { getHighAccuracyLocation } from '@/lib/geo-utils';

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
  const [isCodePopupOpen, setIsCodePopupOpen] = useState(false);
  const [pendingQrCode, setPendingQrCode] = useState<string | null>(null);

  const executeCheckIn = useCallback(
    async (
      qrCode: string,
      body: any,
      onSuccessCallback?: (venueId: string) => Promise<void> | void,
      onErrorCallback?: (venueId: string | null) => void,
    ): Promise<boolean> => {
      const response = await apiClient.post<ApiResult<CheckInResponse>>(
        API_ENDPOINTS.VENUES.QR_CHECK_IN(qrCode),
        body,
      );

      const { venueId } = response.data.result;

      if (response.data.success) {
        setShowCheckIn(true);
        await onSuccessCallback?.(venueId);
        return true;
      } else {
        const errorMessage = response.data?.errors?.[0]?.error;
        toast.error(errorMessage || t('toast.checkInFailed'));
        onErrorCallback?.(venueId);
        return false;
      }
    },
    [t],
  );

  const performCheckIn = useCallback(
    async (
      qrCode: string,
      onSuccessCallback?: (venueId: string) => Promise<void> | void,
      onErrorCallback?: (venueId: string | null) => void,
    ): Promise<boolean> => {
      try {
        const toastId = toast.loading(t('toast.gettingLocation'));
        try {
          const position = await getHighAccuracyLocation();
          toast.dismiss(toastId);
          return await executeCheckIn(
            qrCode,
            {
              latitude: position.latitude,
              longitude: position.longitude,
            },
            onSuccessCallback,
            onErrorCallback,
          );
        } catch (err) {
          toast.dismiss(toastId);
          // Location failed, show code popup
          setPendingQrCode(qrCode);
          setIsCodePopupOpen(true);
          return false;
        }
      } catch (error: any) {
        const errorMessage = extractApiError(error)[0];
        toast.error(errorMessage || t('toast.checkInFailed'));
        onErrorCallback?.(null);
        return false;
      }
    },
    [t, executeCheckIn],
  );

  const performCodeCheckIn = useCallback(
    async (
      qrCode: string,
      pinCode: string,
      onSuccessCallback?: (venueId: string) => Promise<void> | void,
      onErrorCallback?: (venueId: string | null) => void,
    ): Promise<boolean> => {
      try {
        return await executeCheckIn(
          qrCode,
          { pinCode },
          onSuccessCallback,
          onErrorCallback,
        );
      } catch (error: any) {
        const errorMessage = extractApiError(error)[0];
        toast.error(errorMessage || t('toast.checkInFailed'));
        onErrorCallback?.(null);
        return false;
      }
    },
    [executeCheckIn, t],
  );

  const handleCodeSubmit = useCallback(
    async (code: string) => {
      if (!pendingQrCode) return;
      const success = await performCodeCheckIn(
        pendingQrCode,
        code,
        onSuccess,
        onError,
      );
      if (success) {
        setPendingQrCode(null);
        setIsCodePopupOpen(false);
      }
    },
    [pendingQrCode, performCodeCheckIn, onSuccess, onError],
  );

  const handleQrCode = useCallback(
    async (rawValue: string): Promise<boolean> => {
      setIsScannerOpen(false);

      const qrCode = extractQrCode(rawValue, true) || '';
      if (!qrCode) {
        toast.error(t('toast.invalidQr'));
        onError?.(null);
        return false;
      }

      return await performCheckIn(qrCode, onSuccess, onError);
    },
    [t, onError, onSuccess, performCheckIn],
  );

  return {
    isScannerOpen,
    setIsScannerOpen,
    showCheckIn,
    setShowCheckIn,
    isCodePopupOpen,
    setIsCodePopupOpen: (open: boolean) => {
      setIsCodePopupOpen(open);
      if (!open && pendingQrCode) {
        onError?.(null);
        setPendingQrCode(null);
      }
    },
    handleQrCode,
    performCheckIn,
    performCodeCheckIn,
    handleCodeSubmit,
    setPendingQrCode,
  };
}
