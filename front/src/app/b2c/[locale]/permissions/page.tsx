'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES } from '@/lib/routes';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { checkPermissionStatus } from '@/lib/permissions';
import styles from './permissions.module.css';

export default function PermissionsPage() {
  const t = useTranslations('screens.permissions');
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const [permissions, setPermissions] = useState({
    locationAllowed: false,
    notificationAllowed: false,
  });

  useEffect(() => {
    const check = async () => {
      const status = await checkPermissionStatus();
      setPermissions(status);
    };

    check();

    // Re-check when window gains focus as user might have changed settings
    window.addEventListener('focus', check);
    return () => window.removeEventListener('focus', check);
  }, []);

  return (
    <div>
      <Header
        title={t('title')}
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => safeBack(PAGE_ROUTES.PROFILE)}
          />
        }
      />
      <div className={styles.content}>
        <div className={styles.section}>
          <div className={styles.textWrapper}>
            <h3 className={styles.sectionTitle}>{t('locationTitle')}</h3>
            <p className={styles.sectionDesc}>{t('locationDesc')}</p>
          </div>
          {permissions.locationAllowed && (
            <Icon
              src="/assets/permissions/check-mark.svg"
              alt="check"
              width={24}
              height={24}
              className={styles.checkIcon}
              priority
            />
          )}
        </div>

        <div className={styles.section}>
          <div className={styles.textWrapper}>
            <h3 className={styles.sectionTitle}>{t('notificationsTitle')}</h3>
            <p className={styles.sectionDesc}>{t('notificationsDesc')}</p>
          </div>
          {permissions.notificationAllowed && (
            <Icon
              src="/assets/permissions/check-mark.svg"
              alt="check"
              width={24}
              height={24}
              className={styles.checkIcon}
              priority
            />
          )}
        </div>

        <div className={styles.section}>
          <div className={styles.textWrapper}>
            <p className={styles.sectionTitle}>{t('description')}</p>
            <p className={styles.instructions}>{t('instructions')}</p>
          </div>
        </div>
      </div>
    </div>
  );
}
