'use client';

import { useState, useEffect } from 'react';
import Image from 'next/image';
import Script from 'next/script';
import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useAuthStore, RequiredAction } from '@/lib/auth/use-auth-store';
import { useGoogleSignIn } from '@/lib/auth/use-google-sign-in';
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
  const [googleLoading, setGoogleLoading] = useState(false);
  const signin = useAuthStore((state) => state.signin);
  const signinWithGoogle = useAuthStore((state) => state.signinWithGoogle);

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

  const handleGoogleSuccess = async (credential: string) => {
    setGoogleLoading(true);
    try {
      const result = await signinWithGoogle(credential);

      if (result.requiredAction === RequiredAction.OnboardingCompletion) {
        router.push(PAGE_ROUTES.ONBOARDING);
      } else {
        router.push(PAGE_ROUTES.FEED);
      }
    } catch (err: any) {
      toast.error(err.message || tCommonErrors('network'));
    } finally {
      setGoogleLoading(false);
    }
  };

  const { renderButton, isGoogleLoaded, onScriptLoad } = useGoogleSignIn({
    onSuccess: handleGoogleSuccess,
    onError: (error) => toast.error(error.message),
  });

  useEffect(() => {
    if (isGoogleLoaded) {
      renderButton('google-signin-button');
    }
  }, [isGoogleLoaded, renderButton]);

  return (
    <>
      <Script
        src="https://accounts.google.com/gsi/client"
        strategy="afterInteractive"
        onLoad={onScriptLoad}
      />
      <div className={styles.page}>
        <div className={styles.container}>
          <div className="flex w-full justify-center">
            <div className={styles.header}>
              <div className={styles.logoRow}>
                <Image
                  src="/assets/common/logo.webp"
                  alt="H"
                  width={180}
                  height={254}
                  className={styles.logoImage}
                  priority
                />
                <span className={styles.brandName}>oplingsHere</span>
                <Image
                  src="/assets/common/beer-mug.webp"
                  alt="Beer"
                  width={32}
                  height={45}
                  className={styles.beerMug}
                />
              </div>
              <p className={styles.tagline}>{t('tagline')}</p>
            </div>
          </div>

          <form
            onSubmit={handleSubmit(onSubmit)}
            className={styles.formSection}
          >
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
                <div className={styles.fieldError}>
                  {errors.password.message}
                </div>
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
              <div className={styles.dividerText}>{t('orContinueWith')}</div>
              <div id="google-signin-button"></div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
