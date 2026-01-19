'use client';

import { useState } from 'react';
import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useAuthStore } from '@/lib/auth/useAuthStore';
import { Link } from '@/i18n/routing';
import styles from '../login/login.module.css'; // Reusing login styles
import { toast } from 'react-toastify';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Image from 'next/image';

export default function ForgotPasswordPage() {
  const t = useTranslations('screens.forgot');
  const tCommonErrors = useTranslations('common.errors');
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const requestPasswordReset = useAuthStore(
    (state) => state.requestPasswordReset,
  );

  const schema = z.object({
    email: z.email(t('errors.emailInvalid')).min(1, t('errors.emailRequired')),
  });

  type FormData = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      email: '',
    },
  });

  const onSubmit = async (data: FormData) => {
    setLoading(true);

    try {
      await requestPasswordReset(data.email);
      setIsSuccess(true);
    } catch (err: any) {
      toast.error(err.message);
    } finally {
      setLoading(false);
    }
  };

  if (isSuccess) {
    return (
      <div className={styles.page}>
        <div className={styles.container}>
          <div className={styles.header}>
            <div className={styles.logoRow}>
              <div className={styles.logoContainer}>
                <Image
                  src="/assets/logo.png"
                  alt="Hoppyfy Logo"
                  fill
                  className={styles.logoImage}
                  priority
                />
              </div>
              <h1 className={styles.brandName}>oppyfy.me</h1>
            </div>
            <p className="mt-4 text-center text-base">
              {t('successDescription')}
            </p>
          </div>

          <Link href="/login" className="mt-4 text-center text-sm underline">
            {t('backToLogin')}
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.logoRow}>
            <div className={styles.logoContainer}>
              <Image
                src="/assets/logo.png"
                alt="Hoppyfy Logo"
                fill
                className={styles.logoImage}
                priority
              />
            </div>
            <h1 className={styles.brandName}>oppyfy.me</h1>
          </div>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className={styles.formSection}>
          <p className="mt-4 text-center text-xl">{t('title')}</p>
          <p className={styles.tagline}>{t('description')}</p>
          <div>
            <Input
              type="email"
              placeholder={t('email')}
              className={styles.inputCredentials}
              disabled={loading}
              {...register('email')}
            />
            {errors.email && (
              <div className={styles.fieldError}>{errors.email.message}</div>
            )}
          </div>

          <Button
            variant={ButtonVariant.Default}
            className={styles.signInButton}
            type="submit"
            disabled={loading}
          >
            {loading ? 'Loading...' : t('submit')}
          </Button>

          <Link href="/login" className="mt-4 text-center text-sm underline">
            {t('backToLogin')}
          </Link>
        </form>
      </div>
    </div>
  );
}
