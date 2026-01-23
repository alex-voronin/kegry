'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { toast } from 'react-toastify';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { CoverSection } from '@/components/ui/profile-sections/cover-section';
import { cn } from '@/lib/utils';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { useUserProfileStore } from '@/store/use-user-profile-store';
import { uploadImage } from '@/lib/image-utils';
import styles from './cover-image.module.css';

export default function ProfileCoverImagePage() {
  const t = useTranslations('screens.coverImage');
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [coverBlob, setCoverBlob] = useState<Blob | null>(null);

  const { patchProfile, serverProfile: profile } = useUserProfileStore();

  const handleSave = async () => {
    if (!coverBlob) {
      toast.error(t('toast.coverNotSelected'));
      return;
    }

    setIsSubmitting(true);
    try {
      const publicUrl = await uploadImage(
        coverBlob,
        API_ENDPOINTS.USER_PROFILE.COVER_UPLOAD_URL,
      );

      await patchProfile({ coverImageUrl: publicUrl });
      toast.success(t('toast.uploaded'));
      router.push(PAGE_ROUTES.PROFILE);
    } catch (err: any) {
      toast.error(err.message || 'Failed to upload cover image');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleBack = () => {
    router.back();
  };

  return (
    <div className={styles.wrapper}>
      <div className={styles.coverLayer}>
        <CoverSection
          coverBlob={coverBlob}
          onCoverChange={setCoverBlob}
          existingCoverUrl={profile?.coverImageUrl ?? undefined}
        />
      </div>

      <div className={styles.contentLayer}>
        <Header
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

        <div className={cn(styles.footer, coverBlob ? 'mt-auto' : '')}>
          <Button
            variant={ButtonVariant.Default}
            className={styles.saveButton}
            type="button"
            onClick={handleSave}
            disabled={!coverBlob}
            loading={isSubmitting}
          >
            {t('save')}
          </Button>
        </div>
      </div>
    </div>
  );
}
