'use client';

import Image from 'next/image';
import { Link, useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import styles from './login.module.css';

export default function LoginPage() {
  const t = useTranslations('screens.login');
  const router = useRouter();

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

        <div className={styles.formSection}>
          <Input
            type="email"
            placeholder={t('email')}
            className={styles.inputCredentials}
          />

          <div className={styles.passwordWrapper}>
            <Input
              type="password"
              placeholder={t('password')}
              className={styles.inputCredentials}
            />
            <Link href="/forgot" className={styles.forgotLink}>
              {t('forgot')}
            </Link>
          </div>
        </div>

        <Button
          variant={ButtonVariant.Default}
          className={styles.signInButton}
          onClick={() => router.push('/onboarding')}
        >
          {t('signIn')}
        </Button>

        <div className={styles.footer}>
          <div className={styles.divider}>
            <span className={styles.dividerText}>{t('orContinueWith')}</span>
          </div>

          <div className={styles.socialGrid}>
            <Button variant={ButtonVariant.Social}>
              <Image
                src="/assets/google.png"
                alt={t('google')}
                width={24}
                height={24}
                priority
              />
              {t('google')}
            </Button>
            <Button variant={ButtonVariant.Social}>
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
