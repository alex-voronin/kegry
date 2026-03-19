'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useParams } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Header } from '@/components/ui/header';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import stylesCommon from '@/components/ui/common.module.css';
import { Textarea } from '@/components/ui/textarea';
import { useVenueStore } from '../../use-venue-store';
import styles from './create-promotion.module.css';

export default function AddPromotionPage() {
  const t = useTranslations('screens.createPromotion');
  const router = useRouter();
  const params = useParams();
  const venueId = params.id as string;

  const [isSubmitting, setIsSubmitting] = useState(false);
  const { createPromotion } = useVenueStore();

  const schema = z.object({
    body: z.string().trim().min(1, t('validation.bodyRequired')),
  });

  type FormValues = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      body: '',
    },
  });

  const onSubmit = async (values: FormValues) => {
    setIsSubmitting(true);
    try {
      await createPromotion(venueId, values.body);
      router.back();
    } catch (err: any) {
      // Error handled in store
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
              {isSubmitting ? <Spinner /> : t('save')}
            </Button>
          </div>
        </form>
      </div>
    </>
  );
}
