'use client';

import { useState, useEffect } from 'react';
import { useRouter } from '@/i18n/routing';
import { useSearchParams } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import {
  useAuthStore,
  RequiredAction,
  UserType,
} from '@/lib/auth/use-auth-store';
import { Link } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '@/app/b2c/[locale]/login/login.module.css';
import { Logo } from '@/components/common/Logo';
import { toast } from 'react-toastify';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { createLoginSchema } from '@/lib/auth/login-schema';
import { EmailField } from '@/components/auth/fields/EmailField';
import { PasswordField } from '@/components/auth/fields/PasswordField';
import { GoogleSignInButton } from '@/components/auth/google/GoogleSignInButton';
import { B2CUserLoginAttemptDialog } from '@/components/auth/B2CUserLoginAttemptDialog';

export default function LoginPage() {
  const t = useTranslations('screens.login');
  const tCommonErrors = useTranslations('common.errors');
  const router = useRouter();
  const searchParams = useSearchParams();
  const [loading, setLoading] = useState(false);
  const signin = useAuthStore((state) => state.signin);
  const signinWithGoogle = useAuthStore((state) => state.signinWithGoogle);
  const [isB2CDialogOpen, setIsB2CDialogOpen] = useState(false);
  const { accessToken, hasHydrated, requiredAction } = useAuthStore();

  const shouldRedirect = searchParams.get('auto-signin');
  const isRedirecting = hasHydrated && accessToken && shouldRedirect;

  useEffect(() => {
    if (isRedirecting) {
      if (requiredAction === RequiredAction.EmailVerification) {
        router.replace(PAGE_ROUTES.B2B.VERIFY_EMAIL);
      } else {
        router.replace(PAGE_ROUTES.B2B.VENUES);
      }
    } else if (hasHydrated && shouldRedirect) {
      // Clean up the URL if we are not redirecting
      router.replace(PAGE_ROUTES.B2B.LOGIN());
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

  const onSubmit = async (data: FormData) => {
    setLoading(true);

    try {
      const result = await signin(data.email, data.password, [UserType.B2B]);

      if (result.userType === UserType.B2C) {
        setIsB2CDialogOpen(true);
        return;
      }

      if (result.requiredAction === RequiredAction.EmailVerification) {
        router.push(PAGE_ROUTES.B2B.VERIFY_EMAIL);
      } else {
        router.push(PAGE_ROUTES.B2B.VENUES);
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
      const result = await signinWithGoogle(credential, UserType.B2B, [
        UserType.B2B,
      ]);

      if (result.userType === UserType.B2C) {
        setIsB2CDialogOpen(true);
        return;
      }

      router.push(PAGE_ROUTES.B2B.VENUES);
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
          <Logo b2b={true} />

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
                      href={PAGE_ROUTES.B2B.FORGOT_PASSWORD}
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
              <GoogleSignInButton
                onSuccess={handleGoogleSuccess}
                onError={(error) => toast.error(error.message)}
              />
            </div>
          </div>
        </div>
      </div>
      <B2CUserLoginAttemptDialog
        isOpen={isB2CDialogOpen}
        onClose={() => setIsB2CDialogOpen(false)}
      />
    </>
  );
}
