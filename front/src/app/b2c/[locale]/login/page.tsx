'use client';

import { useState, useEffect } from 'react';
import { useRouter } from '@/i18n/routing';
import { useSearchParams } from 'next/navigation';
import { useTranslations, useLocale } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import {
  useAuthStore,
  RequiredAction,
  UserType,
} from '@/lib/auth/use-auth-store';
import { Link } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import { useSettingsStore } from '../settings/use-settings-store';
import styles from './login.module.css';
import { Logo } from '@/components/common/Logo';
import { toast } from 'react-toastify';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { createLoginSchema } from '@/lib/auth/login-schema';
import { EmailField } from '@/components/auth/fields/EmailField';
import { PasswordField } from '@/components/auth/fields/PasswordField';
import { GoogleSignInButton } from '@/components/auth/google/GoogleSignInButton';

export default function LoginPage() {
  const t = useTranslations('screens.login');
  const tCommonErrors = useTranslations('common.errors');
  const router = useRouter();
  const locale = useLocale();
  const searchParams = useSearchParams();
  const [loading, setLoading] = useState(false);
  const signin = useAuthStore((state) => state.signin);
  const signinWithGoogle = useAuthStore((state) => state.signinWithGoogle);
  const { fetchSettings } = useSettingsStore();
  const { accessToken, hasHydrated, requiredAction } = useAuthStore();

  const shouldRedirect = searchParams.get('auto-signin');
  const isRedirecting = hasHydrated && accessToken && shouldRedirect;

  useEffect(() => {
    if (isRedirecting) {
      if (requiredAction === RequiredAction.EmailVerification) {
        router.replace(PAGE_ROUTES.VERIFY_EMAIL);
      } else if (requiredAction === RequiredAction.OnboardingCompletion) {
        router.replace(PAGE_ROUTES.ONBOARDING);
      } else if (requiredAction === RequiredAction.HowItWorksCompletion) {
        router.replace(PAGE_ROUTES.HOW_IT_WORKS);
      } else {
        router.replace(PAGE_ROUTES.FEED);
      }
    } else if (hasHydrated && shouldRedirect) {
      // Clean up the URL if we are not redirecting
      router.replace(PAGE_ROUTES.LOGIN());
    }
  }, [isRedirecting, requiredAction, router, hasHydrated, shouldRedirect]);

  const schema = createLoginSchema(t);

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

  const applyServerLocaleAndOpenFeed = async () => {
    await fetchSettings();
    const serverLocale = useSettingsStore.getState().locale;
    router.push(PAGE_ROUTES.FEED, {
      locale: (serverLocale as 'en' | 'sr') || locale,
    });
  };

  const onSubmit = async (data: FormData) => {
    setLoading(true);

    try {
      const result = await signin(data.email, data.password, [
        UserType.B2C,
        UserType.B2B,
      ]);

      if (result.requiredAction === RequiredAction.EmailVerification) {
        router.push(PAGE_ROUTES.VERIFY_EMAIL);
      } else if (
        result.requiredAction === RequiredAction.OnboardingCompletion
      ) {
        router.push(PAGE_ROUTES.ONBOARDING);
      } else {
        await applyServerLocaleAndOpenFeed();
      }
    } catch (err: any) {
      if (err.code === 'AccountNotFound') {
        toast.error(t('errors.accountNotFound'));
      } else if (err.code === 'ValidationError') {
        toast.error(t('errors.wrongCredentials'));
      } else {
        toast.error(err.message || tCommonErrors('network'));
      }
    } finally {
      setLoading(false);
    }
  };

  const handleGoogleSuccess = async (credential: string) => {
    setLoading(true);
    try {
      const result = await signinWithGoogle(credential, UserType.B2C, [
        UserType.B2C,
        UserType.B2B,
      ]);

      if (result.requiredAction === RequiredAction.OnboardingCompletion) {
        router.push(PAGE_ROUTES.ONBOARDING);
      } else {
        await applyServerLocaleAndOpenFeed();
      }
    } catch (err: any) {
      toast.error(err.message || tCommonErrors('network'));
    } finally {
      setLoading(false);
    }
  };

  if (!hasHydrated || isRedirecting) {
    return null;
  }

  return (
    <>
      <div className={styles.page}>
        <div className={styles.container}>
          <Logo />

          <form
            onSubmit={handleSubmit(onSubmit)}
            className={styles.formSection}
          >
            <div className={styles.inputWrapper}>
              <EmailField
                placeholder={t('email')}
                className={styles.inputCredentials}
                disabled={loading}
                inputProps={register('email')}
              />
              {errors.email && (
                <div className={styles.fieldError}>{errors.email.message}</div>
              )}
            </div>

            <div className={styles.passwordWrapper}>
              <div className={styles.passwordInputWrapper}>
                <PasswordField
                  placeholder={t('password')}
                  className={styles.inputCredentials}
                  disabled={loading}
                  eyeButtonClassName={styles.eyeButton}
                  inputProps={register('password')}
                  extra={
                    <Link
                      href={PAGE_ROUTES.FORGOT_PASSWORD}
                      className={styles.forgotLink}
                    >
                      {t('forgot')}
                    </Link>
                  }
                />
              </div>
              {errors.password && (
                <div className={styles.fieldError}>
                  {errors.password.message}
                </div>
              )}
            </div>

            <div className={styles.actionsRow}>
              <Link href={PAGE_ROUTES.REGISTER} className={styles.toggleLink}>
                {t('switchToRegister')}
              </Link>
              <Button
                variant={ButtonVariant.Default}
                className={styles.signInButton}
                type="submit"
                loading={loading}
              >
                {t('signIn')}
              </Button>
            </div>
          </form>

          <div className={styles.footer}>
            <div className={styles.divider}>
              <div className={styles.dividerText}>{t('orContinueWith')}</div>
              <GoogleSignInButton
                onSuccess={handleGoogleSuccess}
                onError={(error) => toast.error(error.message)}
              />
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
