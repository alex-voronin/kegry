'use client';

import { useState } from 'react';
import Image from 'next/image';
import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { Link } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from './login.module.css';
import { toast } from 'react-toastify';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

export default function LoginPage() {
  const t = useTranslations('screens.login');
  const tCommonErrors = useTranslations('common.errors');
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const signin = useAuthStore((state) => state.signin);

  const schema = z.object({
    email: z.email(t('errors.emailInvalid')).min(1, t('errors.emailRequired')),
    password: z
      .string()
      .min(1, t('errors.passwordRequired'))
      .min(10, t('errors.passwordShort'))
      .refine(
        (pw) => /[A-Z]/.test(pw) && /[a-z]/.test(pw) && /[0-9]/.test(pw),
        { message: t('errors.passwordComplexity') },
      ),
  });

  type FormData = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const onSubmit = async (data: FormData) => {
    setLoading(true);

    try {
      const result = await signin(data.email, data.password);

      if (result.requiredAction === RequiredAction.EmailVerification) {
        router.push(PAGE_ROUTES.VERIFY_EMAIL);
      } else if (
        result.requiredAction === RequiredAction.OnboardingCompletion
      ) {
        router.push(PAGE_ROUTES.ONBOARDING);
      } else {
        router.push(PAGE_ROUTES.FEED);
      }
    } catch (err: any) {
      toast.error(err.message || tCommonErrors('network'));
    } finally {
      setLoading(false);
    }
  };

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
          <p className={styles.tagline}>{t('tagline')}</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className={styles.formSection}>
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

          <div className={styles.passwordWrapper}>
            <Input
              type="password"
              placeholder={t('password')}
              className={styles.inputCredentials}
              disabled={loading}
              {...register('password')}
            />
            <Link
              href={PAGE_ROUTES.FORGOT_PASSWORD}
              className={styles.forgotLink}
            >
              {t('forgot')}
            </Link>
            {errors.password && (
              <div className={styles.fieldError}>{errors.password.message}</div>
            )}
          </div>

          <Button
            variant={ButtonVariant.Default}
            className={styles.signInButton}
            type="submit"
            loading={loading}
          >
            {t('signIn')}
          </Button>
        </form>

        <div className={styles.footer}>
          <div className={styles.divider}>
            <span className={styles.dividerText}>{t('orContinueWith')}</span>
          </div>

          <div className={styles.socialGrid}>
            <Button variant={ButtonVariant.Social} type="button">
              <Image
                src="/assets/google.png"
                alt={t('google')}
                width={24}
                height={24}
                priority
              />
              {t('google')}
            </Button>
            <Button variant={ButtonVariant.Social} type="button">
              <Image
                src="/assets/facebook.png"
                alt={t('facebook')}
                width={24}
                height={24}
                priority
              />
              {t('facebook')}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
