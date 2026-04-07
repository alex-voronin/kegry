'use client';

import { useState } from 'react';
import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import {
  useAuthStore,
  RequiredAction,
  UserType,
} from '@/lib/auth/use-auth-store';
import { Link } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../login/login.module.css';
import { Logo } from '@/components/common/Logo';
import { toast } from 'react-toastify';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { createRegisterSchema } from '@/lib/auth/register-schema';
import { EmailField } from '@/components/auth/fields/EmailField';
import { PasswordField } from '@/components/auth/fields/PasswordField';
import { GoogleSignInButton } from '@/components/auth/google/GoogleSignInButton';

export default function RegisterPage() {
  const t = useTranslations('screens.register');
  const tCommonErrors = useTranslations('common.errors');
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const signup = useAuthStore((state) => state.signup);
  const signinWithGoogle = useAuthStore((state) => state.signinWithGoogle);

  const schema = createRegisterSchema(t);

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
      const result = await signinWithGoogle(credential, UserType.B2C, [
        UserType.B2C,
        UserType.B2B,
      ]);

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
                  eyeButtonClassName={styles.eyeButtonRight}
                  showPassword={showPassword}
                  onToggleShowPassword={() => setShowPassword((v) => !v)}
                  inputProps={register('password')}
                />
              </div>
              {errors.password && (
                <div className={styles.fieldError}>
                  {errors.password.message}
                </div>
              )}
            </div>

            <div className={styles.passwordWrapper}>
              <div className={styles.passwordInputWrapper}>
                <PasswordField
                  placeholder={t('confirmPassword')}
                  className={styles.inputCredentials}
                  disabled={loading}
                  eyeButtonClassName={styles.eyeButtonRight}
                  showPassword={showPassword}
                  onToggleShowPassword={() => setShowPassword((v) => !v)}
                  inputProps={register('confirmPassword')}
                />
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
              <GoogleSignInButton
                onSuccess={handleGoogleSuccess}
                onError={(error) => toast.error(error.message)}
              />
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
