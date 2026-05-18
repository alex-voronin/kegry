'use client';

import { useCallback, useState, useEffect, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { Link } from '@/i18n/routing';
import { X } from 'lucide-react';
import { Scanner } from '@yudiel/react-qr-scanner';
import type {
  IDetectedBarcode,
  IScannerError,
  IScannerHandle,
} from '@yudiel/react-qr-scanner';
import { checkPermissionStatus } from '@/lib/permissions';
import { PAGE_ROUTES } from '@/lib/routes';
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
  checkLocation = false,
}: QrScannerPopupProps) {
  const t = useTranslations('components.qrScannerPopup');
  const [cameraError, setCameraError] = useState(false);
  const [isLocationAllowed, setIsLocationAllowed] = useState(true);
  const scannerRef = useRef<IScannerHandle>(null);

  useEffect(() => {
    if (isOpen && checkLocation) {
      checkPermissionStatus().then(({ locationAllowed }) => {
        setIsLocationAllowed(locationAllowed);
      });
    } else if (isOpen) {
      setIsLocationAllowed(true);
    }
  }, [isOpen, checkLocation]);

  useEffect(() => {
    if (!isOpen) return;

    const video = scannerRef.current?.getVideoElement();
    if (!video) return;

    const applyZoom = () => {
      const stream = video.srcObject as MediaStream | null;
      if (!stream) return;
      const track = stream.getVideoTracks()[0];
      if ((track?.getCapabilities() as Record<string, unknown>)?.zoom) {
        track
          .applyConstraints({ advanced: [{ zoom: 1.8 }] } as unknown as MediaTrackConstraints)
          .catch(() => {});
      }
    };

    video.addEventListener('loadedmetadata', applyZoom);
    video.addEventListener('playing', applyZoom);

    return () => {
      video.removeEventListener('loadedmetadata', applyZoom);
      video.removeEventListener('playing', applyZoom);
    };
  }, [isOpen]);

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
            {cameraError
              ? t('cameraDenied')
              : t.rich('locationDenied', {
                  link: (chunks) => (
                    <Link
                      href={PAGE_ROUTES.PERMISSIONS}
                      className={styles.inlineLink}
                    >
                      {chunks}
                    </Link>
                  ),
                })}
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
            ref={scannerRef}
            components={{
              torch: true,
              finder: true,
              zoom: true,
            }}
            onScan={handleScan}
            onError={(err) => {
              if ((err as IScannerError).kind === 'permission-denied') {
                setCameraError(true);
              }
            }}
            constraints={{
              facingMode: 'environment',
              width: { ideal: 1280 },
              height: { ideal: 720 },
            }}
            formats={['qr_code']}
            scanDelay={300}
            sound={false}
          />
        </div>
      </div>
    </div>
  );
}
