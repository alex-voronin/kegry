'use client';

import { useState, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { Camera } from 'lucide-react';
import { Header } from '@/components/ui/header';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import stylesCommon from '@/components/ui/common.module.css';
import { Input } from '@/components/ui/input';
import { useCompaniesStore } from '../use-companies-store';
import { API_ENDPOINTS, PAGE_ROUTES } from '@/lib/routes';
import { uploadImage, compressImageToBlob } from '@/lib/image-utils';
import styles from './create.module.css';

export default function CompanyCreatePage() {
  const t = useTranslations('screens.createCompany');
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [avatarBlob, setAvatarBlob] = useState<Blob | null>(null);

  const { createCompany, patchCompany } = useCompaniesStore();

  const schema = z.object({
    name: z.string().trim().min(1, t('validation.nameRequired')),
    email: z
      .string()
      .trim()
      .email(t('validation.emailInvalid'))
      .optional()
      .or(z.literal('')),
  });

  type FormValues = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitted },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      email: '',
    },
  });

  const handleAvatarClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      // Show preview immediately
      const reader = new FileReader();
      reader.onloadend = () => {
        setAvatarPreview(reader.result as string);
      };
      reader.readAsDataURL(file);

      // Compress and set blob
      const compressedBlob = await compressImageToBlob(file, 1024 * 1024, 800);
      setAvatarBlob(compressedBlob);
    } catch (err) {
      toast.error('Failed to process image');
    }
  };

  const onSubmit = async (values: FormValues) => {
    if (!avatarBlob) {
      toast.error(t('validation.avatarRequired'));
      return;
    }

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
        API_ENDPOINTS.COMPANIES.AVATAR_UPLOAD_URL(id),
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
    <>
      <Header
        title={t('title')}
        left={
          <Icon
            src="/assets/b2b/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => router.back()}
          />
        }
      />
      <div className={styles.wrapper}>
        <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
          <div className={stylesCommon.sectionContainer}>
            <div className={styles.avatarSection} onClick={handleAvatarClick}>
              <div className={styles.avatarContainer}>
                {avatarPreview ? (
                  <img
                    src={avatarPreview}
                    alt="avatar"
                    className={styles.avatarImage}
                  />
                ) : (
                  <div className={styles.avatarPlaceholderIcon}>
                    <Camera size={40} className="text-secondary opacity-50" />
                    <span className={styles.requiredAsterisk}>*</span>
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
            {isSubmitted && !avatarBlob && (
              <p className={stylesCommon.errorMessage}>
                {t('validation.avatarRequired')}
              </p>
            )}
          </div>

          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle required>{t('name')}</FormSectionTitle>
            <Input
              {...register('name')}
              placeholder={t('name')}
              className={stylesCommon.inputDark}
            />
            {errors.name?.message && (
              <p className={stylesCommon.errorMessage}>{errors.name.message}</p>
            )}
          </div>

          <div className={stylesCommon.sectionContainer}>
            <FormSectionTitle>{t('email')}</FormSectionTitle>
            <Input
              {...register('email')}
              placeholder={t('emailPlaceholder')}
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
              disabled={isSubmitting}
            >
              {isSubmitting ? <Spinner /> : t('submit')}
            </Button>
          </div>
        </form>
      </div>
    </>
  );
}
