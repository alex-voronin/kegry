'use client';

import { useEffect, useRef, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useSafeBack } from '@/hooks/use-safe-back';
import { useParams } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { Camera } from 'lucide-react';
import { Header } from '@/components/ui/header';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Icon } from '@/components/ui/icon';
import stylesCommon from '@/components/ui/common.module.css';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { useCompaniesStore } from '../../use-companies-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage, compressImageToBlob } from '@/lib/image-utils';
import { useCompanyProfileStore } from '../use-company-profile-store';
import styles from '../../create/create.module.css';

export default function CompanyEditPage() {
  const t = useTranslations('screens.editCompany');
  const tCreate = useTranslations('screens.createCompany');
  const router = useRouter();
  const { safeBack } = useSafeBack();
  const { id } = useParams() as { id: string };
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [avatarBlob, setAvatarBlob] = useState<Blob | null>(null);

  const { patchCompany } = useCompaniesStore();
  const { selectedCompany, isLoading, fetchCompany } = useCompanyProfileStore();

  useEffect(() => {
    if (id) fetchCompany(id);
  }, [id, fetchCompany]);

  const schema = z.object({
    name: z.string().trim().min(1, tCreate('validation.nameRequired')),
    email: z
      .string()
      .trim()
      .email(tCreate('validation.emailInvalid'))
      .optional()
      .or(z.literal('')),
  });

  type FormValues = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitted, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', email: '' },
  });

  useEffect(() => {
    if (!selectedCompany) return;
    reset({
      name: selectedCompany.name || '',
      email: selectedCompany.email || '',
    });
  }, [selectedCompany, reset]);

  const handleAvatarClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      const reader = new FileReader();
      reader.onloadend = () => setAvatarPreview(reader.result as string);
      reader.readAsDataURL(file);

      const compressedBlob = await compressImageToBlob(file, 1024 * 1024, 800);
      setAvatarBlob(compressedBlob);
    } catch {
      toast.error('Failed to process image');
    }
  };

  const onSubmit = async (values: FormValues) => {
    const existingAvatarUrl = selectedCompany?.avatarImageUrl || null;
    if (!avatarBlob && !existingAvatarUrl) {
      toast.error(tCreate('validation.avatarRequired'));
      return;
    }

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

      safeBack(PAGE_ROUTES.B2B.COMPANIES_DETAIL(id));
    } catch {
      toast.error(t('toast.updateFailed'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading || !selectedCompany) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  const displayAvatar = avatarPreview || selectedCompany.avatarImageUrl;
  const canSubmit = isDirty || !!avatarBlob;

  return (
    <>
      <Header
        title={t('title')}
        left={
          <Icon
            src="/assets/b2b/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => safeBack(PAGE_ROUTES.B2B.COMPANIES_DETAIL(id))}
          />
        }
      />
      <div className={styles.wrapper}>
        <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
          <div className={stylesCommon.sectionContainer}>
            <div className={styles.avatarSection} onClick={handleAvatarClick}>
              <div className={styles.avatarContainer}>
                {displayAvatar ? (
                  <img
                    src={displayAvatar}
                    alt="avatar"
                    className={styles.avatarImage}
                  />
                ) : (
                  <div className={styles.avatarPlaceholder}>
                    <div className={styles.avatarPlaceholderIcon}>
                      <Camera size={40} className="text-secondary opacity-50" />
                      <span className={styles.requiredAsterisk}>*</span>
                    </div>
                  </div>
                )}
                <input
                  type="file"
                  ref={fileInputRef}
                  className="hidden"
                  accept="image/*"
                  onChange={handleFileChange}
                />
              </div>
            </div>
            {isSubmitted && !avatarBlob && !selectedCompany.avatarImageUrl && (
              <p className={stylesCommon.errorMessage}>
                {tCreate('validation.avatarRequired')}
              </p>
            )}
          </div>

          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle required>{tCreate('name')}</FormSectionTitle>
            <Input
              {...register('name')}
              placeholder={tCreate('name')}
              className={stylesCommon.inputDark}
            />
            {errors.name?.message && (
              <p className={stylesCommon.errorMessage}>{errors.name.message}</p>
            )}
          </div>

          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{tCreate('email')}</FormSectionTitle>
            <Input
              {...register('email')}
              placeholder={tCreate('email')}
              className={stylesCommon.inputDark}
            />
            {errors.email?.message && (
              <p className={stylesCommon.errorMessage}>
                {errors.email.message}
              </p>
            )}
          </div>

          <div className={styles.footer}>
            <Button
              variant={ButtonVariant.Default}
              className={styles.submitButton}
              type="submit"
              disabled={isSubmitting || !canSubmit}
            >
              {isSubmitting ? <Spinner /> : t('submit')}
            </Button>
          </div>
        </form>
      </div>
    </>
  );
}
