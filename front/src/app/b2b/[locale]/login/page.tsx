'use client';

import { useState, useEffect } from 'react';
import Script from 'next/script';
import { useRouter } from '@/i18n/routing';
import { useSearchParams } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Eye, EyeOff } from 'lucide-react';
import { Input } from '@/components/ui/input';
import {
  useAuthStore,
  RequiredAction,
  UserType,
} from '@/lib/auth/use-auth-store';
import { useGoogleSignIn } from '@/lib/auth/use-google-sign-in';
import { Link } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '@/app/b2c/[locale]/login/login.module.css';
import { Logo } from '@/components/common/Logo';
import { toast } from 'react-toastify';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

export default function LoginPage() {
  const t = useTranslations('screens.login');
  const tCommonErrors = useTranslations('common.errors');
  const router = useRouter();
  const searchParams = useSearchParams();
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const signin = useAuthStore((state) => state.signin);
  const signinWithGoogle = useAuthStore((state) => state.signinWithGoogle);
  const { accessToken, hasHydrated, requiredAction } = useAuthStore();

  const shouldRedirect = searchParams.get('auto-signin');
  const isRedirecting = hasHydrated && accessToken && shouldRedirect;

  useEffect(() => {
    if (isRedirecting) {
      if (requiredAction === RequiredAction.EmailVerification) {
        router.replace(PAGE_ROUTES.B2B.VERIFY_EMAIL);
      } else if (requiredAction === RequiredAction.OnboardingCompletion) {
        router.replace(PAGE_ROUTES.B2B.HOME); // Adjust if B2B has its own onboarding
      } else {
        router.replace(PAGE_ROUTES.B2B.HOME);
      }
    } else if (hasHydrated && shouldRedirect) {
      // Clean up the URL if we are not redirecting
      router.replace(PAGE_ROUTES.B2B.LOGIN());
    }
  }, [isRedirecting, requiredAction, router, hasHydrated, shouldRedirect]);

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
        router.push(PAGE_ROUTES.B2B.VERIFY_EMAIL);
      } else {
        // TODO: implement
        router.push(PAGE_ROUTES.B2B.HOME);
      }
    } catch (err: any) {
      if (err.code === 'ValidationError') {
        toast.error(t('errors.wrongCredentials'));
      } else {
        toast.error(err.message);
      }
    } finally {
      setLoading(false);
    }
  };

  const handleGoogleSuccess = async (credential: string) => {
    setLoading(true);
    try {
      const result = await signinWithGoogle(credential, UserType.B2B);
      // TODO: implement
      router.push(PAGE_ROUTES.B2B.HOME);
    } catch (err: any) {
      toast.error(err.message || tCommonErrors('network'));
    } finally {
      setLoading(false);
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

  if (!hasHydrated || isRedirecting) {
    return null;
  }

  return (
    <>
      <Script
        src="https://accounts.google.com/gsi/client"
        strategy="afterInteractive"
        onLoad={onScriptLoad}
      />
      <div className={styles.page}>
        <div className={styles.container}>
          <Logo />

          <form
            onSubmit={handleSubmit(onSubmit)}
            className={styles.formSection}
          >
            <div className={styles.inputWrapper}>
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
              <div className={styles.passwordInputWrapper}>
                <Input
                  type={showPassword ? 'text' : 'password'}
                  placeholder={t('password')}
                  className={styles.inputCredentials}
                  disabled={loading}
                  {...register('password')}
                />
                <button
                  type="button"
                  className={styles.eyeButton}
                  onClick={() => setShowPassword(!showPassword)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                </button>
                <Link
                  href={PAGE_ROUTES.B2B.FORGOT_PASSWORD}
                  className={styles.forgotLink}
                >
                  {t('forgot')}
                </Link>
              </div>
              {errors.password && (
                <div className={styles.fieldError}>
                  {errors.password.message}
                </div>
              )}
            </div>

            <div className={styles.actionsRow}>
              <Link
                href={PAGE_ROUTES.B2B.REGISTER}
                className={styles.toggleLink}
              >
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
              <div id="google-signin-button"></div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
