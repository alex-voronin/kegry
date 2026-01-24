'use client';

import { Button, ButtonVariant } from '@/components/ui/button';
import { Header } from '@/components/ui/header';
import { CoverSection } from '@/components/ui/profile-sections/cover-section';
import { Link, useRouter } from '@/i18n/routing';
import { ApiResult } from '@/lib/api-types';
import { RequiredAction, useAuthStore } from '@/lib/auth/use-auth-store';
import { cn } from '@/lib/utils';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import {
  base64ToBlob,
  useUserProfileStore,
} from '@/store/use-user-profile-store';
import { uploadImage } from '@/lib/image-utils';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import axios from 'axios';
import { useTranslations } from 'next-intl';
import { useState } from 'react';
import { toast } from 'react-toastify';
import styles from './cover.module.css';

export default function CoverPage() {
  const t = useTranslations('screens.onboarding');
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

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

  const { setAuth, userId, accessToken, refreshToken } = useAuthStore();

  const handleContinue = async () => {
    setError(null);
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

      await axios.post(API_ENDPOINTS.USER_PROFILE.PROFILE, {
        firstName,
        lastName,
        bio,
        avatarImageUrl: avatarUrl,
        coverImageUrl: coverUrl,
        birthday,
        gender: gender,
        hobbyIds: selectedHobbyIds,
      });

      setAuth({
        userId: userId!,
        accessToken: accessToken,
        refreshToken: refreshToken,
        requiredAction: RequiredAction.None,
      });

      router.push(PAGE_ROUTES.FEED);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to create profile';
      setError(errorMessage);
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
          {error && <p className={styles.error}>{error}</p>}
          <Button
            variant={ButtonVariant.Default}
            className={styles.continueButton}
            type="button"
            onClick={handleContinue}
            disabled={!coverBlob}
            loading={isSubmitting}
          >
            {t('continue')}
          </Button>
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
