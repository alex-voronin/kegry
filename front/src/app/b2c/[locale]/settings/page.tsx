'use client';

import { useEffect, useState } from 'react';
import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Button, ButtonVariant } from '@/components/ui/button';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Toggle } from '@/components/ui/toggle';
import { useSettingsStore, DistanceUnits } from './use-settings-store';
import { useUserProfileStore } from '@/store/use-user-profile-store';
import { toast } from 'react-toastify';
import styles from './settings.module.css';

export default function SettingsPage() {
  const router = useRouter();
  const t = useTranslations('screens.settings');

  const {
    distanceUnits,
    pushesEnabled,
    emailsEnabled,
    isLoading: isSettingsLoading,
    fetchSettings,
    updateDistanceUnits,
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

  const [isDistanceMenuOpen, setIsDistanceMenuOpen] = useState(false);

  const loadData = async () => {
    try {
      await Promise.all([
        fetchSettings(),
        serverProfile ? Promise.resolve() : fetchMyProfile(),
      ]);
    } catch (error) {
      toast.error(t('fetchFailed'));
    }
  };

  useEffect(() => {
    loadData();
  }, [fetchSettings, fetchMyProfile, serverProfile, t]);

  const handleBack = () => {
    router.back();
  };

  const handleSave = async () => {
    try {
      const promises = [];
      if (hasSettingsChanges) promises.push(saveSettings());
      if (hasPrivacyChanges) promises.push(updatePrivacy(isPublic));

      await Promise.all(promises);
      router.back();
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

  const hasSettingsChanges =
    serverSettings != null &&
    (distanceUnits !== serverSettings?.distanceUnits ||
      pushesEnabled !== serverSettings?.pushesEnabled ||
      emailsEnabled !== serverSettings?.emailsEnabled);

  const hasPrivacyChanges =
    serverProfile != null && isPublic !== serverProfile?.isPublic;

  const hasChanges = hasSettingsChanges || hasPrivacyChanges;

  const isLoading = isSettingsLoading || isProfileLoading;

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
          <h3 className={styles.sectionTitle}>{t('publicAccount')}</h3>
          <div className={styles.group}>
            <div className={styles.groupItem}>
              <div className={styles.itemContent}>
                <span className={styles.itemLabel}>{t('publicAccount')}</span>
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

          <h3 className={styles.sectionTitle}>{t('distanceUnits')}</h3>
          <div
            className={`${styles.item} ${styles.itemClickable}`}
            onClick={() => setIsDistanceMenuOpen(true)}
          >
            <span className={styles.value}>
              {distanceUnits === DistanceUnits.Miles ? t('mi') : t('km')}
            </span>
          </div>

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
      </div>
    </div>
  );
}
