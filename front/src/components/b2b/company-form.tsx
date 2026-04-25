import { useState, useRef, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Camera } from 'lucide-react';
import { toast } from 'react-toastify';

import { Header } from '@/components/ui/header';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Input } from '@/components/ui/input';
import { ImageCropperModal } from '@/components/ui/image-cropper-modal';
import { compressImageToBlob } from '@/lib/image-utils';
import {
  AVATAR_MAX_LONG_SIDE,
  AVATAR_MAX_SIZE_BYTES,
} from '@/lib/constants';

import stylesCommon from '@/components/ui/common.module.css';
import styles from './company-form.module.css';

export interface CompanyFormValues {
  name: string;
  email?: string;
}

export interface CompanyFormProps {
  title: string;
  submitText: string;
  isSubmitting: boolean;
  initialValues?: Partial<CompanyFormValues> & { avatarImageUrl?: string | null };
  requireAvatar: boolean;
  onCancel: () => void;
  onSubmit: (values: CompanyFormValues, avatarBlob: Blob | null) => Promise<void>;
}

export function CompanyForm({
  title,
  submitText,
  isSubmitting,
  initialValues,
  requireAvatar,
  onCancel,
  onSubmit,
}: CompanyFormProps) {
  const t = useTranslations('components.forms.company');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [avatarPreview, setAvatarPreview] = useState<string | null>(
    initialValues?.avatarImageUrl || null,
  );
  const [avatarBlob, setAvatarBlob] = useState<Blob | null>(null);
  const [isCropperOpen, setIsCropperOpen] = useState(false);
  const [rawImageSrc, setRawImageSrc] = useState<string | null>(null);

  const schema = z.object({
    name: z.string().trim().min(1, t('validation.nameRequired')),
    email: z
      .string()
      .trim()
      .email(t('validation.emailInvalid'))
      .optional()
      .or(z.literal('')),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitted, isDirty },
  } = useForm<CompanyFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      email: '',
    },
  });

  useEffect(() => {
    if (initialValues) {
      reset({
        name: initialValues.name || '',
        email: initialValues.email || '',
      });
      if (initialValues.avatarImageUrl) {
        setAvatarPreview(initialValues.avatarImageUrl);
      }
    }
  }, [initialValues, reset]);

  const handleAvatarClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      const reader = new FileReader();
      reader.onloadend = () => {
        setRawImageSrc(reader.result as string);
        setIsCropperOpen(true);
      };
      reader.readAsDataURL(file);

      // Reset input value to allow selecting the same file again
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch {
      toast.error('Failed to process image');
    }
  };

  const handleCropComplete = async (croppedBlob: Blob) => {
    try {
      setAvatarPreview(URL.createObjectURL(croppedBlob));

      const compressedBlob = await compressImageToBlob(
        croppedBlob,
        AVATAR_MAX_SIZE_BYTES,
        AVATAR_MAX_LONG_SIDE,
      );
      setAvatarBlob(compressedBlob);
      setIsCropperOpen(false);
    } catch {
      toast.error('Failed to process cropped image');
    }
  };

  const handleFormSubmit = (values: CompanyFormValues) => {
    if (requireAvatar && !avatarBlob && !initialValues?.avatarImageUrl) {
      toast.error(t('validation.avatarRequired'));
      return;
    }
    onSubmit(values, avatarBlob);
  };

  const canSubmit = isDirty || !!avatarBlob;

  return (
    <>
      <Header
        title={title}
        left={
          <Icon
            src="/assets/b2b/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={onCancel}
          />
        }
      />
      <div className={styles.wrapper}>
        <form className={styles.form} onSubmit={handleSubmit(handleFormSubmit)}>
          <div className={stylesCommon.sectionContainer}>
            <div className={styles.avatarSection} onClick={handleAvatarClick}>
              <div className={styles.avatarContainer}>
                {avatarPreview ? (
                  <>
                    <img
                      src={avatarPreview}
                      alt="avatar"
                      className={styles.avatarImage}
                    />
                  </>
                ) : (
                  <div className={styles.avatarPlaceholderIcon}>
                    <Camera size={40} className="text-secondary opacity-50" />
                    {requireAvatar && !initialValues?.avatarImageUrl && (
                      <span className={styles.requiredAsterisk}>*</span>
                    )}
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
            {isSubmitted && requireAvatar && !avatarBlob && !initialValues?.avatarImageUrl && (
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
              disabled={isSubmitting || !canSubmit}
            >
              {isSubmitting ? <Spinner /> : submitText}
            </Button>
          </div>
        </form>
      </div>

      <ImageCropperModal
        isOpen={isCropperOpen}
        onClose={() => setIsCropperOpen(false)}
        imageSrc={rawImageSrc}
        onCropComplete={handleCropComplete}
        outputSize={{
          width: AVATAR_MAX_LONG_SIDE,
          height: AVATAR_MAX_LONG_SIDE,
        }}
        cropShape="round"
      />
    </>
  );
}
