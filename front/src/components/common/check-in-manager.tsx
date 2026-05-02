'use client';

import { QrScannerPopup } from '@/components/common/qr-scanner-popup';
import { CheckInCodePopup } from '@/components/common/check-in-code-popup';

interface CheckInManagerProps {
  isScannerOpen: boolean;
  onScannerOpenChange: (open: boolean) => void;
  isCodePopupOpen: boolean;
  onCodePopupOpenChange: (open: boolean) => void;
  handleQrCode: (result: string) => Promise<boolean>;
  handleCodeSubmit: (code: string) => Promise<void>;
}

export function CheckInManager({
  isScannerOpen,
  onScannerOpenChange,
  isCodePopupOpen,
  onCodePopupOpenChange,
  handleQrCode,
  handleCodeSubmit,
}: CheckInManagerProps) {
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
      />
    </>
  );
}
