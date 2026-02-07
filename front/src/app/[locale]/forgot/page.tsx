'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { Link } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../login/login.module.css'; // Reusing login styles
import { toast } from 'react-toastify';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Logo } from '@/components/common/Logo';

export default function ForgotPasswordPage() {
  const t = useTranslations('screens.forgot');
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
          <Logo />
          <div className="mt-4 text-center">
            <p className="text-base">{t('successDescription')}</p>
          </div>

          <Link
            href={PAGE_ROUTES.LOGIN}
            className="mt-4 text-center text-sm underline"
          >
            {t('backToLogin')}
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <Logo />

        <form onSubmit={handleSubmit(onSubmit)} className={styles.formSection}>
          <div className="text-center">
            <p className="mt-4 text-xl">{t('title')}</p>
            <p className="text-secondary mt-2 text-sm">{t('description')}</p>
          </div>
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

          <Link
            href={PAGE_ROUTES.LOGIN}
            className="mt-4 text-center text-sm underline"
          >
            {t('backToLogin')}
          </Link>
        </form>
      </div>
    </div>
  );
}
