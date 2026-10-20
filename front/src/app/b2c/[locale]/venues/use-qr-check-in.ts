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

type CheckInBody =
  | { latitude: number; longitude: number }
  | { pinCode: string };

export function useQrCheckIn({ onSuccess, onError }: UseQrCheckInOptions = {}) {
  const t = useTranslations('components.qr-code-reader');
  const [isScannerOpen, setIsScannerOpen] = useState(false);
  const [showCheckIn, setShowCheckIn] = useState(false);
  const [isCodePopupOpen, setIsCodePopupOpen] = useState(false);
  const [pendingQrCode, setPendingQrCode] = useState<string | null>(null);
  const [codeDescription, setCodeDescription] = useState<string | undefined>();
  const [showLocationRequiredDialog, setShowLocationRequiredDialog] =
    useState(false);

  const executeCheckIn = useCallback(
    async (
      qrCode: string,
      body: CheckInBody,
      onSuccessCallback?: (venueId: string) => Promise<void> | void,
    ): Promise<boolean> => {
      const response = await apiClient.post<ApiResult<CheckInResponse>>(
        API_ENDPOINTS.VENUES.QR_CHECK_IN(qrCode),
        body,
      );

      const { venueId } = response.data.result;

      setShowCheckIn(true);
      await onSuccessCallback?.(venueId);
      return true;
    },
    [],
  );

  const performCheckIn = useCallback(
    async (
      qrCode: string,
      onSuccessCallback?: (venueId: string) => Promise<void> | void,
      onErrorCallback?: (venueId: string | null) => void,
    ): Promise<boolean> => {
      const toastId = toast.loading(t('toast.gettingLocation'));

      let position: Awaited<ReturnType<typeof getHighAccuracyLocation>>;
      try {
        position = await getHighAccuracyLocation();
      } catch {
        toast.dismiss(toastId);
        // Location failed, verify if PIN check-in is allowed before showing popup
        try {
          const settingsResponse = await apiClient.get<
            ApiResult<{ isPinCheckInAllowed: boolean }>
          >(API_ENDPOINTS.VENUES.QR_CHECK_IN_SETTINGS(qrCode));

          if (settingsResponse.data.result.isPinCheckInAllowed) {
            setPendingQrCode(qrCode);
            setCodeDescription(undefined);
            setIsCodePopupOpen(true);
          } else {
            setShowLocationRequiredDialog(true);
            onErrorCallback?.(null);
          }
        } catch {
          toast.error(t('toast.checkInFailed'));
          onErrorCallback?.(null);
        }
        return false;
      }

      toast.dismiss(toastId);

      try {
        return await executeCheckIn(
          qrCode,
          {
            latitude: position.latitude,
            longitude: position.longitude,
          },
          onSuccessCallback,
        );
      } catch (error) {
        const [message, , context] = extractApiError(error);
        const venueId =
          typeof context?.venueId === 'string' ? context.venueId : null;

        toast.error(message || t('toast.checkInFailed'));
        onErrorCallback?.(venueId);
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
        return await executeCheckIn(qrCode, { pinCode }, onSuccessCallback);
      } catch (error) {
        const [message, , context] = extractApiError(error);
        const venueId =
          typeof context?.venueId === 'string' ? context.venueId : null;

        toast.error(message || t('toast.checkInFailed'));
        onErrorCallback?.(venueId);
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
    codeDescription,
    setCodeDescription,
    showLocationRequiredDialog,
    dismissLocationRequiredDialog: () => setShowLocationRequiredDialog(false),
  };
}
