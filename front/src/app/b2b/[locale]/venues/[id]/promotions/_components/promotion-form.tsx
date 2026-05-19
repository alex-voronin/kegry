'use client';

import { useEffect, useImperativeHandle, forwardRef } from 'react';

import { useTranslations } from 'next-intl';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectOption } from '@/components/ui/select';
import { PromoIcon, PromoType } from '@/lib/promo-types';
import stylesCommon from '@/components/ui/common.module.css';
import styles from './promotion-form.module.css';

interface PromotionFormValues {
  type: PromoType;
  body: string;
}

export interface PromotionFormHandle {
  submitForm: () => void;
}

interface PromotionFormProps {
  initialValues?: PromotionFormValues;
  onSubmit: (values: PromotionFormValues) => Promise<void>;
  isSubmitting: boolean;
  submitLabel: string;
  onDirtyChange?: (isDirty: boolean) => void;
}

export const PromotionForm = forwardRef<
  PromotionFormHandle,
  PromotionFormProps
>(function PromotionForm(
  { initialValues, onSubmit, isSubmitting, submitLabel, onDirtyChange },
  ref,
) {
  const tForm = useTranslations('components.forms.promotion');
  const tCommon = useTranslations('common');

  const schema = z.object({
    type: z.enum(PromoType, {
      error: tForm('validation.typeRequired'),
    }),
    body: z.string().trim().min(1, tForm('validation.bodyRequired')),
  });

  const {
    register,
    control,
    handleSubmit,
    formState: { errors, isDirty },
  } = useForm<PromotionFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      type: initialValues?.type ?? PromoType.General,
      body: initialValues?.body ?? '',
    },
  });

  useEffect(() => {
    onDirtyChange?.(isDirty);
  }, [isDirty, onDirtyChange]);

  useImperativeHandle(
    ref,
    () => ({
      submitForm: () => {
        handleSubmit(onSubmit)();
      },
    }),
    [handleSubmit, onSubmit],
  );

  const typeOptions: SelectOption[] = Object.values(PromoType).map((type) => ({
    value: type,
    label: tCommon(`promoTypes.${type}`, { defaultValue: type }),
  }));

  const formatOptionLabel = ({ value, label }: SelectOption) => (
    <div className="flex items-center gap-2">
      <PromoIcon type={value as PromoType} size={20} className="text-primary" />
      <span>{label}</span>
    </div>
  );

  return (
    <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
      <div className={stylesCommon.sectionContainer}>
        <FormSectionTitle required>{tForm('type')}</FormSectionTitle>
        <Controller
          name="type"
          control={control}
          render={({ field }) => (
            <Select
              options={typeOptions}
              formatOptionLabel={formatOptionLabel}
              value={typeOptions.find((opt) => opt.value === field.value)}
              onChange={(option: any) => field.onChange(option?.value)}
              error={errors.type?.message}
              placeholder={tForm('typePlaceholder')}
            />
          )}
        />
      </div>

      <div className={stylesCommon.sectionContainer}>
        <FormSectionTitle required>{tForm('body')}</FormSectionTitle>
        <Textarea
          {...register('body')}
          placeholder={tForm('bodyPlaceholder')}
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
});
