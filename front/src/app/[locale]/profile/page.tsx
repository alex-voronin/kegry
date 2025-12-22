'use client';

import { useState } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { cn } from '@/lib/utils';
import styles from './profile.module.css';

export default function ProfilePage() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isDeleteAccountModalOpen, setIsDeleteAccountModalOpen] =
    useState(false);
  const t = useTranslations('screens.profile');
  const tGender = useTranslations('components.profile-sections.gender');
  const tBio = useTranslations('components.profile-sections.bio');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const router = useRouter();

  const menuOptions = [
    {
      icon: '/assets/profile/edit-icon.svg',
      label: t('menu.edit'),
      onClick: () => console.log('Edit clicked'),
    },
    {
      icon: '/assets/profile/set-photo-icon.svg',
      label: t('menu.setPhoto'),
      onClick: () => console.log('Set photo clicked'),
    },
    {
      icon: '/assets/profile/logout-icon.svg',
      label: t('menu.logOut'),
      onClick: () => router.push('/login'),
    },
    {
      icon: '/assets/profile/delete-icon.svg',
      label: t('menu.delete'),
      onClick: () => setIsDeleteAccountModalOpen(true),
      isDestructive: true,
    },
  ];

  const handleEditProfile = () => {
    router.push('/edit-profile');
  };

  const handleSettings = () => {
    router.push('/settings');
  };

  const handleBack = () => {
    router.back();
  };

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
        right={
          <Icon
            src="/assets/chat/more-icon.svg"
            alt="more"
            width={24}
            height={24}
            onClick={() => setIsMenuOpen(true)}
          />
        }
      />

      <div className={styles.wrapper}>
        <div className={styles.avatarSection}>
          <div className={styles.avatarContainer}>
            <Image
              src="/assets/profile/avatar.png"
              alt="Avatar"
              fill
              className={styles.avatarImage}
              priority
            />
          </div>
          <h1 className={styles.userName} onClick={handleEditProfile}>
            Genadiy Srulin
          </h1>
        </div>

        <div className={styles.content}>
          <div className={styles.section} onClick={handleEditProfile}>
            <FormSectionTitle>{tGender('title')}</FormSectionTitle>
            <div className={styles.field}>
              <span>{tGender('male')}</span>
            </div>
          </div>

          <div className={styles.section} onClick={handleEditProfile}>
            <FormSectionTitle>
              {useTranslations('components.profile-sections.age')('title')}
            </FormSectionTitle>
            <div className={styles.field}>
              <span>35</span>
            </div>
          </div>

          <div className={styles.section} onClick={handleEditProfile}>
            <FormSectionTitle>{tBio('title')}</FormSectionTitle>
            <div className={styles.field}>
              <p>
                A real man - I love hunting, fishing, and drinking beer! Praise
                Odin and await Ragnarök.
              </p>
            </div>
          </div>

          <div className={styles.section} onClick={handleEditProfile}>
            <FormSectionTitle>{tHobbies('title')}</FormSectionTitle>
            <div className={cn(styles.field, styles.hobbiesField)}>
              <div className={styles.hobbyTag}>{tHobbies('volleyball')}</div>
              <div className={styles.hobbyTag}>{tHobbies('boxing')}</div>
              <div className={styles.hobbyTag}>{tHobbies('camping')}</div>
            </div>
          </div>

          <div className={styles.settingsSection}>
            <div
              className={cn(styles.field, styles.settingsField)}
              onClick={handleSettings}
            >
              <span>{t('settings')}</span>
              <Icon
                src="/assets/common/disclosure-right-icon.svg"
                alt="chevron"
                width={10}
                height={18}
                priority
              />
            </div>
          </div>
        </div>
      </div>

      <ModalMenu
        isOpen={isMenuOpen}
        onClose={() => setIsMenuOpen(false)}
        options={menuOptions}
      />

      <ModalDialog
        isOpen={isDeleteAccountModalOpen}
        onClose={() => setIsDeleteAccountModalOpen(false)}
        message={t('deleteAccountDialog.title')}
        primaryButtonText={t('deleteAccountDialog.buttonYes')}
        secondaryButtonText={t('deleteAccountDialog.buttonNo')}
        onPrimaryAction={() => console.log('Account deletion is confirmed')}
        onSecondaryAction={() => console.log('Account deletion is canceled')}
      />
    </div>
  );
}
