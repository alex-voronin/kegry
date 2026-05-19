'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { X } from 'lucide-react';
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
} from '@/components/ui/input-otp';
import { Button } from '@/components/ui/button';
import styles from './check-in-code-popup.module.css';

interface CheckInCodePopupProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (code: string) => Promise<void>;
  description?: string;
}

export function CheckInCodePopup({
  isOpen,
  onClose,
  onSubmit,
  description,
}: CheckInCodePopupProps) {
  const t = useTranslations('components.checkInCodePopup');
  const [code, setCode] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setCode('');
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleSubmit = async () => {
    if (code.length !== 4) return;
    setIsLoading(true);
    try {
      await onSubmit(code);
      onClose();
    } catch (err) {
      // Error handling is usually done by the parent via toasts
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.popup} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <h2 className={styles.title}>{t('title')}</h2>
          <button className={styles.closeButton} onClick={onClose}>
            <X size={20} />
          </button>
        </div>
        <div className={styles.content}>
          <p className={styles.description}>
            {description || t('description')}
          </p>
          <div className={styles.otpWrapper}>
            <InputOTP
              maxLength={4}
              value={code}
              onChange={setCode}
              onComplete={handleSubmit}
              autoFocus
            >
              <InputOTPGroup>
                <InputOTPSlot index={0} />
                <InputOTPSlot index={1} />
                <InputOTPSlot index={2} />
                <InputOTPSlot index={3} />
              </InputOTPGroup>
            </InputOTP>
          </div>
          <Button
            className={styles.submitButton}
            onClick={handleSubmit}
            disabled={code.length !== 4 || isLoading}
            loading={isLoading}
          >
            {t('submit')}
          </Button>
        </div>
      </div>
    </div>
  );
}
