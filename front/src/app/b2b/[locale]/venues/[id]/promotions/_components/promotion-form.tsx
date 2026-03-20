'use client';

import { useTranslations } from 'next-intl';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Textarea } from '@/components/ui/textarea';
import stylesCommon from '@/components/ui/common.module.css';
import styles from './promotion-form.module.css';

interface PromotionFormValues {
  body: string;
}

interface PromotionFormProps {
  initialValues?: PromotionFormValues;
  onSubmit: (values: PromotionFormValues) => Promise<void>;
  isSubmitting: boolean;
  submitLabel: string;
  translationNamespace: string;
}

export function PromotionForm({
  initialValues,
  onSubmit,
  isSubmitting,
  submitLabel,
  translationNamespace,
}: PromotionFormProps) {
  const t = useTranslations(`screens.${translationNamespace}`);

  const schema = z.object({
    body: z.string().trim().min(1, t('validation.bodyRequired')),
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<PromotionFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      body: initialValues?.body ?? '',
    },
  });

  return (
    <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
      <div className={stylesCommon.sectionContainer}>
        <FormSectionTitle required>{t('body')}</FormSectionTitle>
        <Textarea
          {...register('body')}
          placeholder={t('bodyPlaceholder')}
          className={stylesCommon.inputDark}
        />
        {errors.body?.message && (
          <p className={stylesCommon.errorMessage}>{errors.body.message}</p>
        )}
      </div>

      <div className={styles.footer}>
        <Button
          variant={ButtonVariant.Default}
          className={styles.submitButton}
          type="submit"
          disabled={isSubmitting}
        >
          {isSubmitting ? <Spinner /> : submitLabel}
        </Button>
      </div>
    </form>
  );
}
