'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import axios from 'axios';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Link } from '@/i18n/routing';
import { CoverSection } from '@/components/ui/profile-sections/cover-section';
import styles from './cover.module.css';
import { Header } from '@/components/ui/header';
import { useUserProfileStore } from '@/store/use-user-profile-store';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import { useAuthStore, RequiredAction } from '@/lib/auth/useAuthStore';
import { ApiResult } from '@/lib/api-types';
import { cn } from '@/lib/utils';
import { toast } from 'react-toastify';

interface PresignedUrlResponse {
  uploadUrl: string;
  publicDownloadUrl: string;
}

async function getPresignedUrl(
  endpoint: string,
): Promise<PresignedUrlResponse> {
  const response = await axios.post<ApiResult<PresignedUrlResponse>>(endpoint);
  if (!response.data.success) {
    throw new Error(
      response.data.errors[0]?.error || 'Failed to get upload URL',
    );
  }
  return response.data.result;
}

async function uploadBlobToS3(uploadUrl: string, blob: Blob): Promise<void> {
  await axios.put(uploadUrl, blob, {
    headers: {
      'Content-Type': blob.type || 'image/jpeg',
    },
  });
}

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
    avatarBlob,
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

      if (!avatarBlob) {
        toast.error(
          'Avatar image is not selected. Please go back and select it.',
        );
        return;
      }
      const avatarPresigned = await getPresignedUrl(
        '/api/v1/user-profile/avatar/upload-url',
      );
      await uploadBlobToS3(avatarPresigned.uploadUrl, avatarBlob);
      avatarUrl = avatarPresigned.publicDownloadUrl;

      if (!coverBlob) {
        toast.error('Choose cover image is not selected.');
        return;
      }
      const coverPresigned = await getPresignedUrl(
        '/api/v1/user-profile/cover/upload-url',
      );
      await uploadBlobToS3(coverPresigned.uploadUrl, coverBlob);
      coverUrl = coverPresigned.publicDownloadUrl;

      await axios.post('/api/v1/user-profile', {
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

      resetProfile();

      router.push('/feed');
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
            disabled={isSubmitting || !coverBlob}
          >
            {isSubmitting ? t('creating') : t('continue')}
          </Button>
          <Link
            href="/onboarding"
            className="mt-4 text-center text-sm underline"
          >
            {t('back')}
          </Link>
        </div>
      </div>
    </div>
  );
}
