'use client';

import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { QrScannerPopup } from '@/components/common/qr-scanner-popup';
import { CheckInCodePopup } from '@/components/common/check-in-code-popup';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { PAGE_ROUTES } from '@/lib/routes';

interface CheckInManagerProps {
  isScannerOpen: boolean;
  onScannerOpenChange: (open: boolean) => void;
  isCodePopupOpen: boolean;
  onCodePopupOpenChange: (open: boolean) => void;
  handleQrCode: (result: string) => Promise<boolean>;
  handleCodeSubmit: (code: string) => Promise<void>;
  codeDescription?: string;
  showLocationRequiredDialog?: boolean;
  onDismissLocationRequiredDialog?: () => void;
}

export function CheckInManager({
  isScannerOpen,
  onScannerOpenChange,
  isCodePopupOpen,
  onCodePopupOpenChange,
  handleQrCode,
  handleCodeSubmit,
  codeDescription,
  showLocationRequiredDialog,
  onDismissLocationRequiredDialog,
}: CheckInManagerProps) {
  const router = useRouter();
  const t = useTranslations('components.check-in-manager');

  return (
    <>
      <QrScannerPopup
        isOpen={isScannerOpen}
        onClose={() => onScannerOpenChange(false)}
        onScan={async (result) => {
          const success = await handleQrCode(result);
          if (success) {
            onScannerOpenChange(false);
          }
        }}
      />
      <CheckInCodePopup
        isOpen={isCodePopupOpen}
        onClose={() => onCodePopupOpenChange(false)}
        onSubmit={handleCodeSubmit}
        description={codeDescription}
      />
      <ModalDialog
        isOpen={!!showLocationRequiredDialog}
        onClose={() => onDismissLocationRequiredDialog?.()}
        message={t('locationRequiredDialog.message')}
        primaryButtonText={t('locationRequiredDialog.later')}
        onPrimaryAction={() => onDismissLocationRequiredDialog?.()}
        secondaryButtonText={t('locationRequiredDialog.setupPermissions')}
        onSecondaryAction={() => {
          onDismissLocationRequiredDialog?.();
          router.push(PAGE_ROUTES.PERMISSIONS);
        }}
      />
    </>
  );
}
