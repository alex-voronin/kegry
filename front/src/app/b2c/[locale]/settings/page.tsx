'use client';

import { useEffect, useState, useRef } from 'react';
import { useRouter, usePathname } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useTranslations, useLocale } from 'next-intl';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Button, ButtonVariant } from '@/components/ui/button';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Toggle } from '@/components/ui/toggle';
import { Spinner } from '@/components/ui/spinner';
import {
  useSettingsStore,
  DistanceUnits,
  SettingsFetchType,
} from './use-settings-store';
import { useUserProfileStore } from '@/store/use-user-profile-store';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { toast } from 'react-toastify';
import styles from './settings.module.css';
import { PAGE_ROUTES } from '@/lib/routes';

const locales = [
  {
    code: 'en',
    label: 'English',
    shortLabel: 'EN',
    flag: '🇬🇧',
  },
  {
    code: 'sr',
    label: 'Srpski',
    shortLabel: 'SR',
    flag: '🇷🇸',
  },
] as const;

export default function SettingsPage() {
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const pathname = usePathname();
  const locale = useLocale();
  const t = useTranslations('screens.settings');

  const {
    distanceUnits,
    pushesEnabled,
    emailsEnabled,
    locale: storeLocale,
    isLoading: isSettingsLoading,
    fetchSettings,
    updateDistanceUnits,
    updateLocale,
    togglePushes,
    toggleEmails,
    saveSettings,
    serverSettings,
  } = useSettingsStore();

  const {
    serverProfile,
    isPublic,
    isLoading: isProfileLoading,
    fetchMyProfile,
    setIsPublic,
    updatePrivacy,
  } = useUserProfileStore();
  const { requiredAction, hasHydrated } = useAuthStore();
  const onboardingRequired =
    requiredAction === RequiredAction.OnboardingCompletion;

  const [isDistanceMenuOpen, setIsDistanceMenuOpen] = useState(false);
  const [isLanguageMenuOpen, setIsLanguageMenuOpen] = useState(false);

  const currentLocale =
    locales.find((l) => l.code === (storeLocale || locale)) || locales[0];

  const handleLocaleChange = (newLocale: 'en' | 'sr') => {
    updateLocale(newLocale);
  };

  const loadData = async () => {
    try {
      if (onboardingRequired) {
        await fetchSettings(SettingsFetchType.OnlyNotifications);
      } else {
        await Promise.all([
          fetchSettings(SettingsFetchType.All),
          serverProfile ? Promise.resolve() : fetchMyProfile(),
        ]);
      }
    } catch (error) {
      toast.error(t('fetchFailed'));
    }
  };

  useEffect(() => {
    if (!hasHydrated) return;

    loadData();
  }, [
    fetchSettings,
    fetchMyProfile,
    serverProfile,
    t,
    hasHydrated,
    RequiredAction,
  ]);

  const handleBack = () => {
    safeBack(PAGE_ROUTES.PROFILE);
  };

  const handleSave = async () => {
    try {
      const promises = [];
      if (hasSettingsChanges) promises.push(saveSettings());
      if (hasPrivacyChanges) promises.push(updatePrivacy(isPublic));

      await Promise.all(promises);
      if (storeLocale && storeLocale !== locale) {
        router.replace(PAGE_ROUTES.PROFILE, { locale: storeLocale });
      } else {
        safeBack(PAGE_ROUTES.PROFILE);
      }
    } catch (error) {
      toast.error(t('saveFailed'));
    }
  };

  const distanceOptions = [
    {
      label: t('kmFull'),
      onClick: () => updateDistanceUnits(DistanceUnits.Kilometers),
    },
    {
      label: t('miFull'),
      onClick: () => updateDistanceUnits(DistanceUnits.Miles),
    },
  ];

  const languageOptions = locales.map((item) => ({
    icon: (
      <span className="flex w-6 items-center justify-center text-[20px] leading-none">
        {item.flag}
      </span>
    ),
    label: `${item.label} (${item.shortLabel})`,
    onClick: () => handleLocaleChange(item.code as 'en' | 'sr'),
  }));

  const hasSettingsChanges =
    serverSettings != null &&
    (distanceUnits !== serverSettings?.distanceUnits ||
      storeLocale !== serverSettings?.locale ||
      pushesEnabled !== serverSettings?.pushesEnabled ||
      emailsEnabled !== serverSettings?.emailsEnabled);

  const hasPrivacyChanges =
    serverProfile != null && isPublic !== serverProfile?.isPublic;

  const hasChanges = hasSettingsChanges || hasPrivacyChanges;

  const isLoading = isSettingsLoading || isProfileLoading;

  if (!hasHydrated) {
    return (
      <>
        <Header
          title={t('title')}
          left={
            <Icon
              src="/assets/common/disclosure-left-icon.svg"
              alt="back"
              width={14}
              height={27}
              onClick={handleBack}
            />
          }
        />
        <div className="flex h-screen items-center justify-center">
          <Spinner />
        </div>
      </>
    );
  }

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
            onClick={handleBack}
          />
        }
      />
      <div className={styles.container}>
        <div className="mt-4">
          {!onboardingRequired && (
            <>
              <h3 className={styles.sectionTitle}>{t('publicAccount')}</h3>
              <div className={styles.group}>
                <div className={styles.groupItem}>
                  <div className={styles.itemContent}>
                    <span className={styles.itemLabel}>
                      {t('publicAccount')}
                    </span>
                    <span className={styles.itemDescription}>
                      {t('publicAccountDesc')}
                    </span>
                  </div>
                  <Toggle
                    checked={isPublic}
                    onChange={setIsPublic}
                    srTitle={t('publicAccount')}
                  />
                </div>
              </div>
            </>
          )}

          <h3 className={styles.sectionTitle}>{t('language')}</h3>
          <div
            className={`${styles.item} ${styles.itemClickable}`}
            onClick={() => setIsLanguageMenuOpen(true)}
          >
            <span className={styles.value}>{currentLocale.label}</span>
          </div>

          {!onboardingRequired && (
            <>
              <h3 className={styles.sectionTitle}>{t('distanceUnits')}</h3>
              <div
                className={`${styles.item} ${styles.itemClickable}`}
                onClick={() => setIsDistanceMenuOpen(true)}
              >
                <span className={styles.value}>
                  {distanceUnits === DistanceUnits.Miles ? t('mi') : t('km')}
                </span>
              </div>
            </>
          )}

          <h3 className={styles.sectionTitle}>{t('notifications')}</h3>
          <div className={styles.group}>
            <div className={styles.groupItem}>
              <div className={styles.itemContent}>
                <span className={styles.itemLabel}>{t('push')}</span>
                <span className={styles.itemDescription}>{t('pushDesc')}</span>
              </div>
              <Toggle checked={pushesEnabled} onChange={togglePushes} />
            </div>
            <div className={styles.groupItem}>
              <div className={styles.itemContent}>
                <span className={styles.itemLabel}>{t('email')}</span>
                <span className={styles.itemDescription}>{t('emailDesc')}</span>
              </div>
              <Toggle checked={emailsEnabled} onChange={toggleEmails} />
            </div>
          </div>
          <div className={styles.footer}>
            <Button
              variant={ButtonVariant.Default}
              onClick={handleSave}
              loading={isLoading}
              disabled={!hasChanges}
              className={styles.saveButton}
            >
              {t('save')}
            </Button>
          </div>
        </div>

        <ModalMenu
          isOpen={isDistanceMenuOpen}
          onClose={() => setIsDistanceMenuOpen(false)}
          options={distanceOptions}
        />
        <ModalMenu
          isOpen={isLanguageMenuOpen}
          onClose={() => setIsLanguageMenuOpen(false)}
          options={languageOptions}
        />
      </div>
    </div>
  );
}
