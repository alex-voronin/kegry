'use client';

import { useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Toggle } from '@/components/ui/toggle';
import { Spinner } from '@/components/ui/spinner';
import { useUserProfileStore } from '@/store/use-user-profile-store';
import { toast } from 'react-toastify';
import styles from './privacy.module.css';

export default function PrivacyPage() {
  const t = useTranslations('screens.privacy');
  const router = useRouter();

  const {
    serverProfile,
    isPublic,
    isLoading,
    fetchMyProfile,
    setIsPublic,
    updatePrivacy,
  } = useUserProfileStore();

  useEffect(() => {
    // Only fetch if we don't have the profile loaded yet
    if (!serverProfile) {
      fetchMyProfile();
    }
  }, [fetchMyProfile, serverProfile]);

  const handleBack = () => {
    router.back();
  };

  const handleToggle = (checked: boolean) => {
    setIsPublic(checked);
  };

  const handleSave = async () => {
    try {
      await updatePrivacy(isPublic);
      router.back();
    } catch (error) {
      toast.error(t('saveFailed'));
    }
  };

  if (isLoading && serverProfile === null) {
    return (
      <div className="flex h-dvh items-center justify-center">
        <Spinner />
      </div>
    );
  }

  const hasChanges =
    serverProfile != null && isPublic !== serverProfile?.isPublic;

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
              <div className="flex min-w-0 flex-1 flex-col gap-1 pr-4">
                <span className={styles.itemLabel}>{t('publicAccount')}</span>
                <span className={styles.itemDescription}>
                  {t('publicAccountDesc')}
                </span>
              </div>
              <div className="shrink-0">
                <Toggle
                  checked={isPublic}
                  onChange={handleToggle}
                  srTitle={t('publicAccount')}
                />
              </div>
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
      </div>
    </div>
  );
}
