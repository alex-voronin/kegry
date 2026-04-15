'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { toast } from 'react-toastify';

import { useCompaniesStore } from '../use-companies-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage } from '@/lib/image-utils';
import { useSafeBack } from '@/hooks/use-safe-back';
import { CompanyForm, CompanyFormValues } from '@/components/b2b/company-form';

export default function CompanyCreatePage() {
  const t = useTranslations('screens.createCompany');
  const router = useRouter();
  const { safeBack } = useSafeBack();

  const [isSubmitting, setIsSubmitting] = useState(false);
  const { createCompany, patchCompany } = useCompaniesStore();

  const handleCancel = () => {
    safeBack(PAGE_ROUTES.B2B.COMPANIES);
  };

  const handleSubmit = async (values: CompanyFormValues, avatarBlob: Blob | null) => {
    if (!avatarBlob) return; // Guaranteed by requireAvatar=true

    setIsSubmitting(true);
    try {
      // 1. Create company
      const { id } = await createCompany(
        values.name,
        values.email || undefined,
      );

      // 2. Upload avatar
      const publicUrl = await uploadImage(
        avatarBlob,
        API_ENDPOINTS.B2B.COMPANIES.AVATAR_UPLOAD_URL(id),
      );

      // 3. Patch company with avatar URL
      await patchCompany(id, { avatarImageUrl: publicUrl });

      router.push(PAGE_ROUTES.B2B.COMPANIES);
    } catch (err: any) {
      toast.error(t('toast.createFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <CompanyForm
      title={t('title')}
      submitText={t('submit')}
      isSubmitting={isSubmitting}
      requireAvatar={true}
      onCancel={handleCancel}
      onSubmit={handleSubmit}
    />
  );
}
