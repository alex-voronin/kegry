'use client';

import { useEffect, useState, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { X } from 'lucide-react';
import { toast } from 'react-toastify';
import { detectInAppBrowser } from '@/lib/detect-in-app-browser';
import { cn } from '@/lib/utils';
import styles from './in-app-browser-banner.module.css';

export function InAppBrowserBanner() {
  const t = useTranslations('common');
  const [isVisible, setIsVisible] = useState(false);
  const [isExiting, setIsExiting] = useState(false);

  useEffect(() => {
    try {
      const dismissed = sessionStorage.getItem(
        'in_app_browser_banner_dismissed',
      );
      if (!detectInAppBrowser() || dismissed) return;
    } catch {
      if (!detectInAppBrowser()) return;
    }
    setIsVisible(true);
  }, []);

  const handleDismiss = useCallback(() => {
    setIsExiting(true);
    setTimeout(() => {
      try {
        sessionStorage.setItem('in_app_browser_banner_dismissed', '1');
      } catch {
        // storage unavailable — banner will re-show on next mount
      }
      setIsVisible(false);
    }, 300);
  }, []);

  const handleCopy = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(window.location.href);
    } catch {
      const textarea = document.createElement('textarea');
      textarea.value = window.location.href;
      textarea.style.position = 'fixed';
      textarea.style.opacity = '0';
      document.body.appendChild(textarea);
      textarea.select();
      const copied = document.execCommand('copy');
      document.body.removeChild(textarea);
      if (!copied) {
        toast.error(t('errors.network'));
        return;
      }
    }
    toast.success(t('toast.linkCopied'));
  }, [t]);

  return (
    <div
      className={cn(
        styles.banner,
        isVisible && styles.visible,
        isExiting && styles.exiting,
      )}
    >
      <div className={styles.topRow}>
        <p className={styles.message}>{t('inAppBrowser.message')}</p>
        <button
          type="button"
          className={styles.dismissBtn}
          onClick={handleDismiss}
          aria-label={t('inAppBrowser.dismiss')}
        >
          <X size={20} />
        </button>
      </div>
      <button
        type="button"
        className={styles.copyBtn}
        onClick={handleCopy}
      >
        {t('inAppBrowser.copyLink')}
      </button>
    </div>
  );
}
