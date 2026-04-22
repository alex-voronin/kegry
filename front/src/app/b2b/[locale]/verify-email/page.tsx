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
import styles from '@/app/b2c/[locale]/login/login.module.css';
import { toast } from 'react-toastify';
import { Logo } from '@/components/common/Logo';

const OTP_LENGTH = 6;

export default function VerifyEmailPage() {
  const t = useTranslations('screens.verifyEmail');
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [value, setValue] = useState('');

  const confirmEmail = useAuthStore((state) => state.confirmEmail);
  const resendConfirmationEmail = useAuthStore(
    (state) => state.resendConfirmationEmail,
  );
  const userId = useAuthStore((state) => state.userId);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();

    if (value.length !== OTP_LENGTH) {
      return;
    }

    if (!userId) {
      toast.error(t('errors.userNotFound'));
      router.push(PAGE_ROUTES.B2B.LOGIN());
      return;
    }

    setLoading(true);

    try {
      await confirmEmail(userId, value);
      router.push(PAGE_ROUTES.B2B.COMPANIES);
    } catch (err: any) {
      if (err.code === 'ValidationError') {
        toast.error(t('errors.invalidCode'));
      } else {
        toast.error(err.message);
      }
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    if (!userId) {
      toast.error(t('errors.userNotFound'));
      router.push(PAGE_ROUTES.B2B.LOGIN());
      return;
    }

    setResending(true);

    try {
      await resendConfirmationEmail(userId);
      toast.success(t('resendSuccess'));
    } catch (err: any) {
      if (err.code === 'CooldownActive') {
        toast.warning(t('resendCooldown'));
      } else {
        toast.error(err.message);
      }
    } finally {
      setResending(false);
    }
  };

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <Logo b2b={true} />
        <div className="mt-6 flex flex-col gap-2 px-8">
          <p className="text-secondary text-lg">{t('description')}</p>
          <p className="text-secondary mt-2 text-sm">
            {t('resendHint')}{' '}
            <button
              type="button"
              onClick={handleResend}
              disabled={resending}
              className="text-primary cursor-pointer bg-transparent font-semibold underline hover:opacity-80 disabled:opacity-50"
            >
              {t('resend').toLowerCase()}
            </button>
          </p>
        </div>

        <form onSubmit={handleSubmit} className={`${styles.formSection}`}>
          <div className="flex justify-center">
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

          <div className="mt-4 flex justify-center">
            <Button
              variant={ButtonVariant.Default}
              className="w-[200px]"
              type="submit"
              disabled={loading || value.length !== OTP_LENGTH}
            >
              {loading ? t('verifying') : t('submit')}
            </Button>
          </div>

          <Link
            href={PAGE_ROUTES.B2B.LOGIN()}
            className="mt-4 text-center text-sm underline"
          >
            {t('backToLogin')}
          </Link>
        </form>
      </div>
    </div>
  );
}
