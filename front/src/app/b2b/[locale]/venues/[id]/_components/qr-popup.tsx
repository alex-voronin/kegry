'use client';

import { useEffect, useRef } from 'react';
import { X, Download } from 'lucide-react';
import styles from './qr-popup.module.css';

interface QrPopupProps {
  isOpen: boolean;
  onClose: () => void;
  url: string;
}

export function QrPopup({ isOpen, onClose, url }: QrPopupProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const qrInstanceRef = useRef<any>(null);

  const handleSave = () => {
    if (qrInstanceRef.current) {
      qrInstanceRef.current.download({
        name: 'qr_check-in',
        extension: 'png',
      });
    }
  };

  useEffect(() => {
    if (!isOpen || !containerRef.current) return;

    let cancelled = false;

    import('qr-code-styling').then(({ default: QRCodeStyling }) => {
      if (cancelled || !containerRef.current) return;

      // Clear previous QR
      containerRef.current.innerHTML = '';

      const qr = new QRCodeStyling({
        width: 256,
        height: 256,
        type: 'svg',
        data: url,
        image: '/assets/b2b/qr-logo.webp',
        backgroundOptions: {
          color: '#333333',
          round: 0.1,
        },
        dotsOptions: {
          color: 'white',
          type: 'square',
        },
        cornersSquareOptions: {
          type: 'extra-rounded',
          color: '#ffab10',
        },
        cornersDotOptions: {
          type: 'dot',
          color: 'white',
        },
        qrOptions: {
          errorCorrectionLevel: 'Q',
        },
        imageOptions: {
          imageSize: 0.45,
          margin: 2,
        },
      });

      qr.append(containerRef.current);
      qrInstanceRef.current = qr;
    });

    return () => {
      cancelled = true;
    };
  }, [isOpen, url]);

  if (!isOpen) return null;

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.popup} onClick={(e) => e.stopPropagation()}>
        <button className={styles.closeButton} onClick={onClose}>
          <X size={20} />
        </button>
        <button className={styles.saveButton} onClick={handleSave}>
          <Download size={20} />
        </button>
        <div className={styles.qrContainer} ref={containerRef} />
      </div>
    </div>
  );
}
