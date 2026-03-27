'use client';

import { useEffect, useRef } from 'react';
import { X } from 'lucide-react';
import styles from './qr-popup.module.css';

interface QrPopupProps {
  isOpen: boolean;
  onClose: () => void;
  url: string;
}

export function QrPopup({ isOpen, onClose, url }: QrPopupProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const qrInstanceRef = useRef<any>(null);

  useEffect(() => {
    if (!isOpen || !containerRef.current) return;

    let cancelled = false;

    import('qr-code-styling').then(({ default: QRCodeStyling }) => {
      if (cancelled || !containerRef.current) return;

      // Clear previous QR
      containerRef.current.innerHTML = '';

      const qr = new QRCodeStyling({
        width: 240,
        height: 240,
        type: 'svg',
        data: url,
        image: '/assets/b2b/qr-logo.webp',
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
        backgroundOptions: {
          color: 'transparent',
        },
        qrOptions: {
          errorCorrectionLevel: 'Q',
        },
        imageOptions: {
          imageSize: 0.45,
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
        <div className={styles.qrContainer} ref={containerRef} />
      </div>
    </div>
  );
}
