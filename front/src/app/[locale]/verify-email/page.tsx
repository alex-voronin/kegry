'use client';

import { useState } from 'react';
import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
} from '@/components/ui/input-otp';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { Link } from '@/i18n/routing';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../login/login.module.css'; // Reusing login styles
import { toast } from 'react-toastify';
import { Logo } from '@/components/common/Logo';

const OTP_LENGTH = 6;

export default function VerifyEmailPage() {
  const t = useTranslations('screens.verifyEmail');
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [value, setValue] = useState('');

  const confirmEmail = useAuthStore((state) => state.confirmEmail);
  const userId = useAuthStore((state) => state.userId);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();

    if (value.length !== OTP_LENGTH) {
      return;
    }

    if (!userId) {
      toast.error(t('errors.userNotFound'));
      router.push(PAGE_ROUTES.LOGIN());
      return;
    }

    setLoading(true);

    try {
      await confirmEmail(userId, value);
      toast.success(t('successTitle'));
      router.push(PAGE_ROUTES.ONBOARDING);
    } catch (err: any) {
      toast.error(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <Logo />
        <div className="mt-2 flex flex-col items-center gap-2">
          <h2 className="text-center text-xl font-medium">{t('title')}</h2>
          <p className="text-secondary text-center text-sm">
            {t('description')}
          </p>
        </div>

        <form onSubmit={handleSubmit} className={`${styles.formSection} !mt-4`}>
          <div className="flex justify-center py-4">
            <InputOTP
              maxLength={OTP_LENGTH}
              value={value}
              onChange={(val) => setValue(val)}
              disabled={loading}
              autoFocus
            >
              <InputOTPGroup>
                <InputOTPSlot index={0} />
                <InputOTPSlot index={1} />
                <InputOTPSlot index={2} />
                <InputOTPSlot index={3} />
                <InputOTPSlot index={4} />
                <InputOTPSlot index={5} />
              </InputOTPGroup>
            </InputOTP>
          </div>

          <Button
            variant={ButtonVariant.Default}
            className={styles.signInButton}
            type="submit"
            disabled={loading || value.length !== OTP_LENGTH}
          >
            {loading ? t('verifying') : t('submit')}
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
