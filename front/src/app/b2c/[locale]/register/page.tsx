'use client';

import { useState, useEffect } from 'react';
import Script from 'next/script';
import { useRouter } from '@/i18n/routing';
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
import styles from '../login/login.module.css';
import { Logo } from '@/components/common/Logo';
import { toast } from 'react-toastify';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

export default function RegisterPage() {
  const t = useTranslations('screens.register');
  const tCommonErrors = useTranslations('common.errors');
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const signup = useAuthStore((state) => state.signup);
  const signinWithGoogle = useAuthStore((state) => state.signinWithGoogle);

  const schema = z
    .object({
      email: z
        .email(t('errors.emailInvalid'))
        .min(1, t('errors.emailRequired')),
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
      message: t('errors.passwordMismatch'),
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
      email: '',
      password: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (data: FormData) => {
    setLoading(true);

    try {
      const result = await signup(data.email, data.password, UserType.B2C);

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
    setLoading(true);
    try {
      const result = await signinWithGoogle(credential, UserType.B2C);

      if (result.requiredAction === RequiredAction.OnboardingCompletion) {
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
                  className={styles.eyeButtonRight}
                  onClick={() => setShowPassword(!showPassword)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                </button>
              </div>
              {errors.password && (
                <div className={styles.fieldError}>
                  {errors.password.message}
                </div>
              )}
            </div>

            <div className={styles.passwordWrapper}>
              <div className={styles.passwordInputWrapper}>
                <Input
                  type={showPassword ? 'text' : 'password'}
                  placeholder={t('confirmPassword')}
                  className={styles.inputCredentials}
                  disabled={loading}
                  {...register('confirmPassword')}
                />
                <button
                  type="button"
                  className={styles.eyeButtonRight}
                  onClick={() => setShowPassword(!showPassword)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                </button>
              </div>
              {errors.confirmPassword && (
                <div className={styles.fieldError}>
                  {errors.confirmPassword.message}
                </div>
              )}
            </div>

            <div className={styles.actionsRow}>
              <Link href={PAGE_ROUTES.LOGIN()} className={styles.toggleLink}>
                {t('switchToLogin')}
              </Link>
              <Button
                variant={ButtonVariant.Default}
                className={styles.signInButton}
                type="submit"
                loading={loading}
              >
                {t('signUp')}
              </Button>
            </div>
          </form>

          <div className={styles.footer}>
            <div className={styles.divider}>
              <div className={styles.dividerText}>{t('orContinueWith')}</div>
              <div id="google-signin-button"></div>
            </div>
            <div className={styles.legalText}>
              {t.rich('legal.text', {
                age: (chunks) => (
                  <span className={styles.legalLink}>{chunks}</span>
                ),
                terms: (chunks) => (
                  <Link
                    href={PAGE_ROUTES.LEGAL.TERMS}
                    className={styles.legalLink}
                  >
                    {chunks}
                  </Link>
                ),
                privacy: (chunks) => (
                  <Link
                    href={PAGE_ROUTES.LEGAL.PRIVACY}
                    className={styles.legalLink}
                  >
                    {chunks}
                  </Link>
                ),
              })}
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
