'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { Link } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from './AgeVerification.module.css';

const STORAGE_KEY = 'age_verified';

export function AgeVerification() {
  const t = useTranslations('components.ageVerification');
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    // Check if user has already verified their age
    const verified = localStorage.getItem(STORAGE_KEY);
    if (!verified) {
      setIsVisible(true);
    }
  }, []);

  const handleConfirm = () => {
    localStorage.setItem(STORAGE_KEY, 'true');
    setIsVisible(false);
  };

  if (!isVisible) {
    return null;
  }

  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <p className={styles.message}>{t('message')}</p>
        <button onClick={handleConfirm} className={styles.confirmButton}>
          {t('confirm')}
        </button>
        <div className={styles.links}>
          <Link href={PAGE_ROUTES.LEGAL.TERMS} className={styles.link}>
            {t('termsOfService')}
          </Link>
          <Link href={PAGE_ROUTES.LEGAL.PRIVACY} className={styles.link}>
            {t('privacyPolicy')}
          </Link>
        </div>
      </div>
    </div>
  );
}
