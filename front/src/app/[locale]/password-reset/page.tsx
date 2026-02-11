'use client';

import { useState, useEffect } from 'react';
import { useRouter } from '@/i18n/routing';
import { useSearchParams } from 'next/navigation';
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

export default function PasswordResetPage() {
  const t = useTranslations('screens.passwordReset');
  const router = useRouter();
  const searchParams = useSearchParams();
  const [loading, setLoading] = useState(false);
  const confirmPasswordReset = useAuthStore(
    (state) => state.confirmPasswordReset,
  );

  const email = searchParams.get('email');
  const token = searchParams.get('token');

  const schema = z
    .object({
      password: z
        .string()
        .min(1, t('errors.passwordRequired'))
        .min(10, t('errors.passwordShort'))
        .refine(
          (pw) => /[A-Z]/.test(pw) && /[a-z]/.test(pw) && /[0-9]/.test(pw),
          { message: t('errors.passwordComplexity') },
        ),
      confirmPassword: z.string().min(1, t('errors.passwordRequired')),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('errors.match'),
      path: ['confirmPassword'],
    });

  type FormData = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      password: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (data: FormData) => {
    if (!email || !token) {
      toast.error(t('errors.invalidLink'));
      return;
    }

    setLoading(true);

    try {
      await confirmPasswordReset(email, token, data.password);
      toast.success(t('success'));
      router.push(PAGE_ROUTES.LOGIN());
    } catch (err: any) {
      toast.error(err.message);
    } finally {
      setLoading(false);
    }
  };

  if (!email || !token) {
    return (
      <div className={styles.page}>
        <div className={styles.container}>
          <div className="text-center">
            <h1 className="text-xl font-bold">Invalid Link</h1>
            <p className="text-muted-foreground mt-2">
              This password reset link is invalid or incomplete.
            </p>
            <Link href={PAGE_ROUTES.LOGIN()} className="mt-4 inline-block">
              <Button variant={ButtonVariant.Default}>
                {t('backToLogin')}
              </Button>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <Logo />

        <form onSubmit={handleSubmit(onSubmit)} className={styles.formSection}>
          <div>
            <Input
              type="password"
              placeholder={t('newPassword')}
              className={styles.inputCredentials}
              disabled={loading}
              {...register('password')}
            />
            {errors.password && (
              <div className={styles.fieldError}>{errors.password.message}</div>
            )}
          </div>

          <div>
            <Input
              type="password"
              placeholder={t('confirmPassword')}
              className={styles.inputCredentials}
              disabled={loading}
              {...register('confirmPassword')}
            />
            {errors.confirmPassword && (
              <div className={styles.fieldError}>
                {errors.confirmPassword.message}
              </div>
            )}
          </div>

          <Button
            variant={ButtonVariant.Default}
            className={styles.signInButton}
            type="submit"
            loading={loading}
          >
            {t('submit')}
          </Button>

          <Link
            href={PAGE_ROUTES.LOGIN()}
            className="mt-4 text-center text-sm underline"
          >
            {t('backToLogin')}
          </Link>
        </form>
      </div>
    </div>
  );
}
