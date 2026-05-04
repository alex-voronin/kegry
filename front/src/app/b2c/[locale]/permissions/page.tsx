'use client';

import { useState, useEffect, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES } from '@/lib/routes';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import {
  checkPermissionStatus,
  requestLocationPermission,
  requestNotificationPermission,
} from '@/lib/permissions';
import { Check, CircleOff, ChevronDown, ChevronUp } from 'lucide-react';
import { SafariShareIcon } from '@/components/ui/safari-share-icon';
import { TuneIcon } from '@/components/ui/tune-icon';
import { SafariSiteSettingsIcon } from '@/components/ui/safari-site-settings-icon';
import { getOS } from '@/lib/os-utils';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
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
  const [isIosTroubleExpanded, setIsIosTroubleExpanded] = useState(false);
  const [activeGroup, setActiveGroup] = useState<
    'location' | 'notifications' | null
  >(null);
  const { requiredAction } = useAuthStore();
  const isOnboarded = requiredAction === RequiredAction.None;

  const iosLocationRef = useRef<HTMLDivElement>(null);
  const iosNotificationsRef = useRef<HTMLDivElement>(null);
  const androidRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!activeGroup) return;

    const scroll = () => {
      if (os === 'ios') {
        if (activeGroup === 'location') {
          iosLocationRef.current?.scrollIntoView({
            behavior: 'smooth',
            block: 'start',
          });
        } else {
          iosNotificationsRef.current?.scrollIntoView({
            behavior: 'smooth',
            block: 'start',
          });
        }
      } else {
        androidRef.current?.scrollIntoView({
          behavior: 'smooth',
          block: 'start',
        });
      }
    };

    const timeout = setTimeout(scroll, 100);
    return () => clearTimeout(timeout);
  }, [activeGroup, os]);

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
        <div
          className={styles.section}
          onClick={async () => {
            if (!permissions.locationAllowed) {
              await requestLocationPermission();
              const status = await checkPermissionStatus();
              setPermissions(status);
              if (!status.locationAllowed) {
                router.push(PAGE_ROUTES.FEED);
              }
            }
            setTimeout(() => setActiveGroup('location'), 250);
          }}
        >
          <div className={styles.textWrapper}>
            <h3 className={styles.sectionTitle}>{t('locationTitle')}</h3>
            <p className={styles.sectionDesc}>
              {isOnboarded ? t('locationDesc') : t('locationDescNotOnboarded')}
            </p>
            <p className={styles.tapInstruction}>{t('tapToView')}</p>
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

        <div
          className={styles.section}
          onClick={async () => {
            if (!permissions.notificationAllowed) {
              await requestNotificationPermission();
              const status = await checkPermissionStatus();
              setPermissions(status);
            }
            setTimeout(() => setActiveGroup('notifications'), 250);
          }}
        >
          <div className={styles.textWrapper}>
            <h3 className={styles.sectionTitle}>{t('notificationsTitle')}</h3>
            <p className={styles.sectionDesc}>{t('notificationsDesc')}</p>
            <p className={styles.tapInstruction}>{t('tapToView')}</p>
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
        {(os === 'ios' || os === 'other' || os === null) && activeGroup && (
          <div className={styles.platformSection}>
            <h2 className={styles.platformTitle}>{t('ios.title')}</h2>

            {activeGroup === 'location' && (
              <div className={styles.instructionGroup} ref={iosLocationRef}>
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
                <div
                  className={styles.disclosureToggle}
                  onClick={() => setIsIosTroubleExpanded(!isIosTroubleExpanded)}
                >
                  <span>{t('ios.location.trouble')}</span>
                  {isIosTroubleExpanded ? (
                    <ChevronUp size={16} />
                  ) : (
                    <ChevronDown size={16} />
                  )}
                </div>

                {isIosTroubleExpanded && (
                  <div className={styles.disclosureContent}>
                    <p className={styles.instructionText}>
                      5. {t('ios.location.step5')}
                      <SafariSiteSettingsIcon className={styles.inlineIcon} />
                    </p>
                    <p className={styles.instructionText}>
                      6. {t('ios.location.step6')}
                    </p>
                    <p className={styles.instructionText}>
                      7. {t('ios.location.step7')}
                    </p>
                    <p className={styles.instructionText}>
                      8. {t('ios.location.step8')}
                    </p>
                  </div>
                )}
              </div>
            )}

            {activeGroup === 'notifications' && (
              <div
                className={styles.instructionGroup}
                ref={iosNotificationsRef}
              >
                <h4 className={styles.instructionSubtitle}>
                  {t('ios.notifications.title')}
                </h4>
                <p className={styles.instructionText}>
                  {t('ios.notifications.desc')}
                </p>
                <p className={styles.instructionText}>
                  1. {t('ios.notifications.step1')}
                  <SafariShareIcon className={styles.inlineIcon} />
                </p>
                <p className={styles.instructionText}>
                  2. {t('ios.notifications.step2')}
                </p>
              </div>
            )}
          </div>
        )}

        {/* Instructions - Android & Desktop */}
        {(os === 'android' || os === 'other' || os === null) && activeGroup && (
          <div className={styles.platformSection}>
            <h2 className={styles.platformTitle}>
              {t('androidDesktop.title')}
            </h2>

            <div className={styles.instructionGroup} ref={androidRef}>
              <h4 className={styles.instructionSubtitle}>
                {t('androidDesktop.titleAll')}
              </h4>
              <p className={styles.instructionText}>
                1. {t('androidDesktop.step1')}
                <TuneIcon className={styles.inlineIcon} />
              </p>
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
