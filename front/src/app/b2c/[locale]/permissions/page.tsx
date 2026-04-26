'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES } from '@/lib/routes';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { checkPermissionStatus } from '@/lib/permissions';
import { Check, CircleOff } from 'lucide-react';
import { SafariShareIcon } from '@/components/ui/safari-share-icon';
import { TuneIcon } from '@/components/ui/tune-icon';
import { getOS } from '@/lib/os-utils';
import styles from './permissions.module.css';

export default function PermissionsPage() {
  const t = useTranslations('screens.permissions');
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const [permissions, setPermissions] = useState({
    locationAllowed: false,
    notificationAllowed: false,
  });
  const [os, setOs] = useState<'ios' | 'android' | 'other' | null>(null);

  useEffect(() => {
    setOs(getOS());
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
        {/* Status Section */}
        <div className={styles.section}>
          <div className={styles.textWrapper}>
            <h3 className={styles.sectionTitle}>{t('locationTitle')}</h3>
            <p className={styles.sectionDesc}>{t('locationDesc')}</p>
          </div>
          <div
            className={
              permissions.locationAllowed
                ? styles.statusIconAllowed
                : styles.statusIconDenied
            }
          >
            {permissions.locationAllowed ? (
              <Check size={24} />
            ) : (
              <CircleOff size={24} />
            )}
          </div>
        </div>

        <div className={styles.section}>
          <div className={styles.textWrapper}>
            <h3 className={styles.sectionTitle}>{t('notificationsTitle')}</h3>
            <p className={styles.sectionDesc}>{t('notificationsDesc')}</p>
          </div>
          <div
            className={
              permissions.notificationAllowed
                ? styles.statusIconAllowed
                : styles.statusIconDenied
            }
          >
            {permissions.notificationAllowed ? (
              <Check size={24} />
            ) : (
              <CircleOff size={24} />
            )}
          </div>
        </div>

        {/* Instructions - iOS */}
        {(os === 'ios' || os === 'other' || os === null) && (
          <div className={styles.platformSection}>
            <h2 className={styles.platformTitle}>{t('ios.title')}</h2>

            <div className={styles.instructionGroup}>
              <h4 className={styles.instructionSubtitle}>
                {t('ios.notifications.title')}
              </h4>
              <p className={styles.instructionText}>
                {t('ios.notifications.desc')}
              </p>
              <div className={styles.stepRow}>
                <p className={styles.instructionText}>
                  1. {t('ios.notifications.step1')}
                </p>
                <SafariShareIcon className="text-primary" />
              </div>
              <p className={styles.instructionText}>
                2. {t('ios.notifications.step2')}
              </p>
            </div>

            <div className={styles.instructionGroup}>
              <h4 className={styles.instructionSubtitle}>
                {t('ios.location.title')}
              </h4>
              <p className={styles.instructionText}>
                1. {t('ios.location.step1')}
              </p>
              <p className={styles.instructionText}>
                2. {t('ios.location.step2')}
              </p>
              <p className={styles.instructionText}>
                3. {t('ios.location.step3')}
              </p>
              <p className={styles.instructionText}>
                4. {t('ios.location.step4')}
              </p>
            </div>
          </div>
        )}

        {/* Instructions - Android & Desktop */}
        {(os === 'android' || os === 'other' || os === null) && (
          <div className={styles.platformSection}>
            <h2 className={styles.platformTitle}>
              {t('androidDesktop.title')}
            </h2>

            <div className={styles.instructionGroup}>
              <h4 className={styles.instructionSubtitle}>
                {t('androidDesktop.titleAll')}
              </h4>
              <div className={styles.stepRow}>
                <p className={styles.instructionText}>
                  1. {t('androidDesktop.step1')}
                </p>
                <TuneIcon className="text-primary" />
              </div>
              <p className={styles.instructionText}>
                2. {t('androidDesktop.step2')}
              </p>
              <p className={styles.instructionText}>
                3. {t('androidDesktop.step3')}
              </p>
              <p className={styles.instructionText}>
                4. {t('androidDesktop.step4')}
              </p>
              <p className={styles.instructionText}>
                5.{' '}
                {t.rich('androidDesktop.step5', {
                  link: (chunks) => (
                    <a
                      href="intent:#Intent;action=android.settings.LOCATION_SOURCE_SETTINGS;end"
                      className={styles.inlineLink}
                    >
                      {chunks}
                    </a>
                  ),
                })}
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
