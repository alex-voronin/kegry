'use client';

import { useState, useEffect, useRef } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { toast } from 'react-toastify';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { Spinner } from '@/components/ui/spinner';
import AvatarCropper from '@/components/ui/avatar-cropper';
import { cn } from '@/lib/utils';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { openTawkChat } from '@/components/tawk-messenger';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import {
  useUserProfileStore,
  calculateAge,
  blobToBase64,
} from '@/store/use-user-profile-store';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import { uploadImage } from '@/lib/image-utils';
import { checkIfPermissionsDenied } from '@/lib/permissions';
import { BRAND_NAME } from '@/lib/constants';
import styles from './profile.module.css';

export default function ProfilePage() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isDeleteAccountModalOpen, setIsDeleteAccountModalOpen] =
    useState(false);
  const [isCropperOpen, setIsCropperOpen] = useState(false);
  const [selectedImage, setSelectedImage] = useState<string | null>(null);
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const t = useTranslations('screens.profile');
  const tGender = useTranslations('components.profile-sections.gender');
  const tBio = useTranslations('components.profile-sections.bio');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const tAge = useTranslations('components.profile-sections.age');
  const router = useRouter();

  const {
    serverProfile: profile,
    firstName: localFirstName,
    lastName: localLastName,
    birthday: localBirthday,
    bio: localBio,
    gender: localGender,
    avatarBase64: localAvatarBase64,
    selectedHobbyIds: localSelectedHobbyIds,
    isLoading,
    fetchMyProfile,
    patchProfile,
    updatePrivacy,
    setAvatarBase64,
  } = useUserProfileStore();

  const { hobbies } = useHobbiesStore();
  const logout = useAuthStore((state) => state.logout);

  const isInitialized = useRef(false);
  const [showPermissionsLink, setShowPermissionsLink] = useState(false);

  useEffect(() => {
    const checkPermissions = async () => {
      const isDenied = await checkIfPermissionsDenied();
      setShowPermissionsLink(isDenied);
    };

    checkPermissions();
  }, []);

  useEffect(() => {
    if (isInitialized.current) return;
    isInitialized.current = true;

    const loadProfile = async () => {
      try {
        await fetchMyProfile();
      } catch (err: any) {
        toast.error(err.message || t('loadFailed'));
      }
    };
    loadProfile();
  }, [fetchMyProfile, t]);

  const handleLogout = async () => {
    await logout();
    window.location.href = PAGE_ROUTES.LOGIN();
  };

  const menuOptions = [
    {
      icon: '/assets/profile/privacy-icon.svg',
      label: profile?.isPublic ? t('menu.makePrivate') : t('menu.makePublic'),
      onClick: async () => {
        try {
          await updatePrivacy(!profile?.isPublic);
          toast.success(
            profile?.isPublic ? t('menu.madePrivate') : t('menu.madePublic'),
          );
        } catch (error) {
          toast.error(t('menu.privacyError'));
        }
      },
    },
    {
      icon: '/assets/profile/edit-icon.svg',
      label: t('menu.edit'),
      onClick: () => router.push(PAGE_ROUTES.PROFILE_EDIT),
    },
    {
      icon: '/assets/profile/set-photo-icon.svg',
      label: t('menu.setAvatar'),
      onClick: () => fileInputRef.current?.click(),
    },
    {
      icon: '/assets/profile/set-photo-icon.svg',
      label: t('menu.setCover'),
      onClick: () => router.push(PAGE_ROUTES.PROFILE_COVER),
    },
    {
      icon: '/assets/profile/logout-icon.svg',
      label: t('menu.logOut'),
      onClick: handleLogout,
    },
    {
      icon: '/assets/common/delete-icon.svg',
      label: t('menu.delete'),
      onClick: () => setIsDeleteAccountModalOpen(true),
      isDestructive: true,
    },
  ];

  const handleEditProfile = () => {
    router.push(PAGE_ROUTES.PROFILE_EDIT);
  };

  const handleBack = () => {
    router.back();
  };

  const handleAvatarClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      setSelectedImage(reader.result as string);
      setIsCropperOpen(true);
    };
    reader.readAsDataURL(file);

    // Reset input so same file can be selected again
    e.target.value = '';
  };

  const handleCropComplete = async (croppedBlob: Blob) => {
    setIsCropperOpen(false);
    setIsUploadingAvatar(true);

    try {
      const base64 = await blobToBase64(croppedBlob);
      setAvatarBase64(base64);
      const publicUrl = await uploadImage(
        croppedBlob,
        API_ENDPOINTS.USER_PROFILE.AVATAR_UPLOAD_URL,
      );

      await patchProfile({ avatarImageUrl: publicUrl });
      toast.success('Avatar updated');
    } catch (err: any) {
      toast.error(err.message || 'Failed to upload avatar');
    } finally {
      setIsUploadingAvatar(false);
    }
  };

  if (isLoading && !profile && !localFirstName) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  const displayName =
    profile?.firstName && profile?.lastName
      ? `${profile.firstName} ${profile.lastName}`
      : localFirstName && localLastName
        ? `${localFirstName} ${localLastName}`
        : '';

  const userBirthday = profile?.birthday || localBirthday;
  const age = userBirthday ? calculateAge(userBirthday) : null;

  const userGender = profile?.gender || localGender;
  const genderLabel = userGender
    ? tGender(userGender === 'Male' ? 'male' : 'female')
    : '';

  const userBio = profile?.bio || localBio;
  const userAvatarUrl = profile?.avatarImageUrl || localAvatarBase64;
  const userHobbies =
    profile?.hobbies ||
    hobbies.filter((h) => localSelectedHobbyIds.includes(h.id));

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
            src="/assets/common/more-icon.svg"
            alt="more"
            width={24}
            height={24}
            onClick={() => setIsMenuOpen(true)}
          />
        }
      />

      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        className="hidden"
        onChange={handleFileSelect}
      />

      <div className={styles.wrapper}>
        <div className={styles.avatarSection}>
          <div className={styles.avatarContainer} onClick={handleAvatarClick}>
            {isUploadingAvatar ? (
              <div className="flex h-full w-full items-center justify-center">
                <Spinner />
              </div>
            ) : (
              <Image
                src={userAvatarUrl || '/assets/common/avatar-placeholder.webp'}
                alt="Avatar"
                fill
                className={styles.avatarImage}
                priority
              />
            )}
          </div>
          <h1 className={styles.userName} onClick={handleEditProfile}>
            {displayName}
          </h1>
        </div>

        <div className={styles.content}>
          <div className={styles.section} onClick={handleEditProfile}>
            <FormSectionTitle>{tGender('title')}</FormSectionTitle>
            <div className={styles.field}>
              <span>{genderLabel}</span>
            </div>
          </div>

          <div className={styles.section} onClick={handleEditProfile}>
            <FormSectionTitle>{tAge('title')}</FormSectionTitle>
            <div className={styles.field}>
              <span>{age ?? '--'}</span>
            </div>
          </div>

          <div className={styles.section} onClick={handleEditProfile}>
            <FormSectionTitle>{tBio('title')}</FormSectionTitle>
            <div className={styles.field}>
              <p>{userBio || '--'}</p>
            </div>
          </div>

          <div className={styles.section} onClick={handleEditProfile}>
            <FormSectionTitle>{tHobbies('title')}</FormSectionTitle>
            <div className={cn(styles.field, styles.hobbiesField)}>
              {userHobbies.map((hobby) => (
                <div key={hobby.id} className={styles.hobbyTag}>
                  {tHobbies(hobby.name as any) || hobby.name}
                </div>
              ))}
              {(!userHobbies || userHobbies.length === 0) && <span>--</span>}
            </div>
          </div>

          <div className={styles.actionsSection}>
            <div
              className={cn(styles.field, styles.actionField)}
              onClick={() => router.push(PAGE_ROUTES.FRIENDS_AND_FOES)}
            >
              <span>{t('friendsAndFoes')}</span>
              <Icon
                src="/assets/common/disclosure-right-icon.svg"
                alt="chevron"
                width={10}
                height={18}
                priority
              />
            </div>
            <div
              className={cn(styles.field, styles.actionField)}
              onClick={() => router.push(PAGE_ROUTES.SETTINGS)}
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
            <div
              className={cn(styles.field, styles.actionField)}
              onClick={() => router.push(PAGE_ROUTES.PRIVACY_SETTINGS)}
            >
              <span>{t('privacy')}</span>
              <Icon
                src="/assets/common/disclosure-right-icon.svg"
                alt="chevron"
                width={10}
                height={18}
                priority
              />
            </div>
            <div
              className={cn(styles.field, styles.actionField)}
              onClick={() => router.push(PAGE_ROUTES.PROFILE_COVER)}
            >
              <span>{t('coverImage')}</span>
              <Icon
                src="/assets/common/disclosure-right-icon.svg"
                alt="chevron"
                width={10}
                height={18}
                priority
              />
            </div>
            <div
              className={cn(styles.field, styles.actionField)}
              onClick={openTawkChat}
            >
              <span>{t('contactUs')}</span>
              <Icon
                src="/assets/common/disclosure-right-icon.svg"
                alt="chevron"
                width={10}
                height={18}
                priority
              />
            </div>
            {showPermissionsLink && (
              <div
                className={cn(styles.field, styles.actionField)}
                onClick={() => router.push(PAGE_ROUTES.PERMISSIONS)}
              >
                <div className={styles.permissionsLabel}>
                  <span>{t('permissions')}</span>
                  <div className={styles.permissionsBadge} />
                </div>
                <Icon
                  src="/assets/common/disclosure-right-icon.svg"
                  alt="chevron"
                  width={10}
                  height={18}
                  priority
                />
              </div>
            )}
            <div
              className={cn(styles.field, styles.actionField, 'mt-8')}
              onClick={handleLogout}
            >
              <span>{t('menu.logOut')}</span>
            </div>
            <div className={styles.version}>
              {BRAND_NAME} for Web v{process.env.NEXT_PUBLIC_APP_VERSION}
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

      {isCropperOpen && selectedImage && (
        <AvatarCropper
          imageSrc={selectedImage}
          onCrop={handleCropComplete}
          onClose={() => setIsCropperOpen(false)}
        />
      )}
    </div>
  );
}
