'use client';

import Link from 'next/link';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../landing.module.css';
import mobileStyles from '../mobile-landing.module.css';

export default function MobileLayout() {
  const t = useTranslations('screens.landing');

  return (
    <div className={styles.wrapper}>
      {/* Header */}
      <header className={styles.header}>
        <div className={styles.logoContainer}>
          <span className={styles.logoText}>
            <Image
              src="/assets/landing/header-logo.webp"
              alt="H"
              width={24}
              height={44}
              className={styles.logoIconImage}
            />
            oplingsHere
            <Image
              src="/assets/landing/header-mug.webp"
              alt="Beer"
              width={24}
              height={24}
            />
          </span>
        </div>
        <Link href={PAGE_ROUTES.LOGIN} className={styles.loginButton}>
          {t('header.nav.loginMobile')}
        </Link>
      </header>

      {/* Cheers Section */}
      <section className={`${styles.cheers} ${mobileStyles.mobileCheers}`}>
        <div className={styles.card}>
          <h1 className={styles.cardTitle}>{t('cheers.title')}</h1>
          <p className={styles.cardDescription}>
            {t('cheers.mobileDescription')}
          </p>
          <Link href={PAGE_ROUTES.LOGIN} className={styles.loginButton}>
            {t('cheers.mobileCta')}
          </Link>
        </div>

        <div className={styles.sideImage}>
          <Image
            src="/assets/landing/section-cheers.webp"
            alt="Cheers"
            width={300}
            height={260}
            className={styles.imagePlaceholder}
          />
        </div>
      </section>

      {/* How It Works */}
      <section
        className={`${styles.howItWorksSection} ${mobileStyles.mobileHowItWorks}`}
      >
        <div className={styles.card}>
          <h2 className={styles.cardTitle}>{t('howItWorks.title')}</h2>
          <div className={mobileStyles.mobileSteps}>
            <div className={mobileStyles.mobileStep}>
              <p className={styles.stepTitle}>{t('howItWorks.mobileStep1')}</p>
              <Image
                src="/assets/landing/step-1.webp"
                alt="Step 1"
                width={150}
                height={100}
                className={styles.stepImagePlaceholder}
              />
            </div>
            <div className={mobileStyles.mobileStep}>
              <p className={styles.stepTitle}>{t('howItWorks.mobileStep2')}</p>
              <Image
                src="/assets/landing/step-2.webp"
                alt="Step 2"
                width={150}
                height={100}
                className={styles.stepImagePlaceholder}
              />
            </div>
            <div className={mobileStyles.mobileStep}>
              <p className={styles.stepTitle}>{t('howItWorks.mobileStep3')}</p>
              <Image
                src="/assets/landing/step-3.webp"
                alt="Step 3"
                width={150}
                height={100}
                className={styles.stepImagePlaceholder}
              />
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className={`${styles.footer} ${mobileStyles.mobileFooter}`}>
        <span className={styles.footerCopyright}>{t('footer.copyright')}</span>
        <div className={mobileStyles.mobileFooterLinks}>
          <Link href={PAGE_ROUTES.LEGAL.PRIVACY} className={styles.footerLink}>
            {t('footer.mobilePrivacy')}
          </Link>
          <Link href={PAGE_ROUTES.LEGAL.CONSENT} className={styles.footerLink}>
            {t('footer.mobileConsent')}
          </Link>
          <Link href={PAGE_ROUTES.LEGAL.TERMS} className={styles.footerLink}>
            {t('footer.mobileTerms')}
          </Link>
          <Link
            href={PAGE_ROUTES.LEGAL.GUIDELINES}
            className={styles.footerLink}
          >
            {t('footer.mobileCommunity')}
          </Link>
        </div>
      </footer>
    </div>
  );
}
