'use client';

import { Button, ButtonVariant } from '@/components/ui/button';
import { Header } from '@/components/ui/header';
import { CoverSection } from '@/components/ui/profile-sections/cover-section';
import { Link, useRouter } from '@/i18n/routing';
import { cn } from '@/lib/utils';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import {
  base64ToBlob,
  useUserProfileStore,
} from '@/store/use-user-profile-store';
import { uploadImage } from '@/lib/image-utils';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import apiClient from '@/lib/api-client';
import { useTranslations } from 'next-intl';
import { useState } from 'react';
import { toast } from 'react-toastify';
import styles from './cover.module.css';

export default function CoverPage() {
  const t = useTranslations('screens.onboarding');
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    firstName,
    lastName,
    birthday,
    bio,
    gender,
    avatarBase64,
    coverBlob,
    setCoverBlob,
    reset: resetProfile,
  } = useUserProfileStore();

  const { selectedHobbyIds, reset: resetHobbies } = useHobbiesStore();

  const handleContinue = async () => {
    setIsSubmitting(true);

    try {
      let avatarUrl = '';
      let coverUrl = '';

      if (!avatarBase64) {
        toast.error(
          'Avatar image is not selected. Please go back and select it.',
        );
        setIsSubmitting(false);
        return;
      }
      const avatarBlob = base64ToBlob(avatarBase64);
      avatarUrl = await uploadImage(
        avatarBlob,
        API_ENDPOINTS.USER_PROFILE.AVATAR_UPLOAD_URL,
      );

      if (!coverBlob) {
        toast.error('Choose cover image is not selected.');
        setIsSubmitting(false);
        return;
      }

      coverUrl = await uploadImage(
        coverBlob,
        API_ENDPOINTS.USER_PROFILE.COVER_UPLOAD_URL,
      );

      await apiClient.post(API_ENDPOINTS.USER_PROFILE.PROFILE, {
        firstName,
        lastName,
        bio,
        avatarImageUrl: avatarUrl,
        coverImageUrl: coverUrl,
        birthday,
        gender: gender,
        hobbyIds: selectedHobbyIds,
      });

      router.push(PAGE_ROUTES.HOW_IT_WORKS);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to create profile';
      toast.error(errorMessage);
      setIsSubmitting(false);
    }
  };

  return (
    <div className={styles.wrapper}>
      <div className={styles.coverLayer}>
        <CoverSection coverBlob={coverBlob} onCoverChange={setCoverBlob} />
      </div>

      <div className={styles.contentLayer}>
        {!coverBlob && <Header title={t('uploadCoverTitle')} />}

        <div className={cn(styles.footer, !coverBlob ? '' : 'mt-auto')}>
          <Button
            variant={ButtonVariant.Default}
            className={styles.createProfileButton}
            type="button"
            onClick={handleContinue}
            disabled={!coverBlob}
            loading={isSubmitting}
          >
            {t('createProfile')}
          </Button>

          <div className={styles.legalText}>
            {t.rich('legal.text', {
              age: (chunks) => (
                <Link
                  href={PAGE_ROUTES.LEGAL.TERMS}
                  className={styles.legalLink}
                  target="_blank"
                >
                  {chunks}
                </Link>
              ),
              terms: (chunks) => (
                <Link
                  href={PAGE_ROUTES.LEGAL.TERMS}
                  className={styles.legalLink}
                  target="_blank"
                >
                  {chunks}
                </Link>
              ),
              privacy: (chunks) => (
                <Link
                  href={PAGE_ROUTES.LEGAL.PRIVACY}
                  className={styles.legalLink}
                  target="_blank"
                >
                  {chunks}
                </Link>
              ),
            })}
          </div>
          <Link
            href={PAGE_ROUTES.ONBOARDING}
            className="mt-4 text-center text-sm underline"
          >
            {t('back')}
          </Link>
        </div>
      </div>
    </div>
  );
}
