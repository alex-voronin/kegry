'use client';

import { useCallback, useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { X } from 'lucide-react';
import { Scanner } from '@yudiel/react-qr-scanner';
import type { IDetectedBarcode } from '@yudiel/react-qr-scanner';
import { checkPermissionStatus } from '@/lib/permissions';
import styles from './qr-scanner-popup.module.css';

interface QrScannerPopupProps {
  isOpen: boolean;
  onClose: () => void;
  onScan: (url: string) => void;
  checkLocation?: boolean;
}

export function QrScannerPopup({
  isOpen,
  onClose,
  onScan,
  checkLocation = true,
}: QrScannerPopupProps) {
  const t = useTranslations('components.qrScannerPopup');
  const [cameraError, setCameraError] = useState(false);
  const [isLocationAllowed, setIsLocationAllowed] = useState(!checkLocation);

  useEffect(() => {
    if (isOpen && checkLocation) {
      checkPermissionStatus().then(({ locationAllowed }) => {
        setIsLocationAllowed(locationAllowed);
      });
    } else if (isOpen) {
      setIsLocationAllowed(true);
    }
  }, [isOpen, checkLocation]);

  const handleScan = useCallback(
    (detectedCodes: IDetectedBarcode[]) => {
      const raw = detectedCodes[0]?.rawValue;
      if (raw) {
        onScan(raw);
      }
    },
    [onScan],
  );

  if (!isOpen) return null;

  if (cameraError || !isLocationAllowed) {
    return (
      <div className={styles.overlay} onClick={onClose}>
        <div
          className={`${styles.popup} ${styles.deniedPopup}`}
          onClick={(e) => e.stopPropagation()}
        >
          <div className={styles.header}>
            <h2 className={styles.title}>{t('title')}</h2>
            <button className={styles.closeButton} onClick={onClose}>
              <X size={20} />
            </button>
          </div>
          <div className={styles.deniedMessage}>
            {cameraError ? t('cameraDenied') : t('locationDenied')}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.popup} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <h2 className={styles.title}>{t('title')}</h2>
          <button className={styles.closeButton} onClick={onClose}>
            <X size={20} />
          </button>
        </div>
        <div className={styles.scannerWrapper}>
          <Scanner
            components={{
              torch: true,
              finder: false,
            }}
            onScan={handleScan}
            onError={(err) => {
              const name = (err as Error)?.name ?? '';
              if (
                name === 'NotAllowedError' ||
                name === 'PermissionDeniedError'
              ) {
                setCameraError(true);
              }
            }}
            constraints={{ facingMode: 'environment' }}
            scanDelay={400}
            sound={false}
          />
        </div>
      </div>
    </div>
  );
}
