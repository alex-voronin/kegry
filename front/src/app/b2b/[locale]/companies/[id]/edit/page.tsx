'use client';

import { useEffect, useState, useMemo, useRef, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useParams } from 'next/navigation';
import { toast } from 'react-toastify';

import { Spinner } from '@/components/ui/spinner';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { useCompaniesStore } from '../../use-companies-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage } from '@/lib/image-utils';
import {
  CompanyForm,
  CompanyFormValues,
  CompanyFormHandle,
} from '@/components/b2b/company-form';

export default function CompanyEditPage() {
  const t = useTranslations('screens.editCompany');
  const tCommon = useTranslations('common');
  const { safeBack } = useSafeBack();
  const { id } = useParams() as { id: string };

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const formRef = useRef<CompanyFormHandle>(null);

  const {
    selectedCompany,
    isProfileLoading: isLoading,
    fetchCompany,
    patchCompany,
  } = useCompaniesStore();

  useEffect(() => {
    if (id) fetchCompany(id);
  }, [id, fetchCompany]);

  const goBack = useCallback(() => {
    safeBack(PAGE_ROUTES.B2B.COMPANIES_DETAIL(id));
  }, [safeBack, id]);

  const handleCancel = () => {
    if (isDirty) {
      setShowSaveDialog(true);
    } else {
      goBack();
    }
  };

  const handleSubmit = async (
    values: CompanyFormValues,
    avatarBlob: Blob | null,
  ) => {
    setIsSubmitting(true);
    try {
      let avatarImageUrl: string | undefined;
      if (avatarBlob) {
        avatarImageUrl = await uploadImage(
          avatarBlob,
          API_ENDPOINTS.B2B.COMPANIES.AVATAR_UPLOAD_URL(id),
        );
      }

      await patchCompany(id, {
        name: values.name,
        email: values.email || undefined,
        ...(avatarImageUrl ? { avatarImageUrl } : {}),
      });

      goBack();
    } catch {
      toast.error(t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const initialValues = useMemo(
    () => ({
      name: selectedCompany?.name || '',
      email: selectedCompany?.email || '',
      avatarImageUrl: selectedCompany?.avatarImageUrl || null,
    }),
    [
      selectedCompany?.name,
      selectedCompany?.email,
      selectedCompany?.avatarImageUrl,
    ],
  );

  if (isLoading || !selectedCompany) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  return (
    <>
      <CompanyForm
        ref={formRef}
        title={t('title')}
        submitText={t('submit')}
        isSubmitting={isSubmitting}
        requireAvatar={false}
        initialValues={initialValues}
        onCancel={handleCancel}
        onSubmit={handleSubmit}
        onDirtyChange={setIsDirty}
      />
      <ModalDialog
        isOpen={showSaveDialog}
        onClose={() => setShowSaveDialog(false)}
        message={tCommon('saveChangesDialog.message')}
        primaryButtonText={tCommon('saveChangesDialog.no')}
        secondaryButtonText={tCommon('saveChangesDialog.yes')}
        onPrimaryAction={goBack}
        onSecondaryAction={() => {
          formRef.current?.submitForm();
        }}
      />
    </>
  );
}
