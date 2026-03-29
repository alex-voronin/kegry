'use client';

import Link from 'next/link';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { PAGE_ROUTES } from '@/lib/routes';
import { LanguageSelector } from './language-selector';
import SocialLinks from './social-links';
import { cn } from '@/lib/utils';
import styles from './desktop-landing.module.css';

export default function DesktopLayout() {
  const t = useTranslations('screens.landing');
  const tf = useTranslations('screens.landing.footer');

  const scrollToSection = (sectionId: string) => {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth' });
    }
  };

  return (
    <div className={styles.wrapper}>
      {/* Fixed Header */}
      <header className={styles.header}>
        <div className={styles.logoContainer}>
          <Image
            src="/assets/b2b/landing/header-logo.webp"
            alt="Kegrycorn"
            width={56}
            height={77}
            className={styles.logoIconImage}
          />
          <div className={styles.logoTextWrapper}>
            <div className={styles.logoTitleRow}>
              <span className={`${styles.logoText}`}>Kegrycorn</span>
            </div>
            <span className={styles.logoSubtitle}>{t('header.subtitle')}</span>
          </div>
        </div>
        <nav className={styles.nav}>
          <span
            className={styles.navLink}
            onClick={() => scrollToSection('features')}
          >
            {t('header.nav.features')}
          </span>
          <span
            className={styles.navLink}
            onClick={() => scrollToSection('contact-us')}
          >
            {t('header.nav.contactUs')}
          </span>
          <div className={styles.navActions}>
            <LanguageSelector />
            <Link
              href={PAGE_ROUTES.B2B.LOGIN(true)}
              className={styles.loginButton}
            >
              {t('header.login')}
            </Link>
          </div>
        </nav>
      </header>

      {/* Hero Section */}
      <section className={styles.hero}>
        <div className={styles.card}>
          <h1 className={styles.cardTitle}>{t('hero.title')}</h1>
          <p className={styles.cardDescription}>{t('hero.description')}</p>
        </div>
        <div className={styles.sideImage}>
          <Image
            src="/assets/b2b/landing/section-hero.webp"
            alt="Corn Graph"
            width={250}
            height={250}
          />
        </div>
      </section>

      {/* Features Grid Section (3 columns) */}
      <section id="features" className={styles.featuresSection}>
        <div className={styles.featuresGrid}>
          {/* Feature 1 */}
          <div className={styles.featureCard}>
            <h2 className={styles.featureTitle}>{t('features.one.title')}</h2>
            <p className={styles.featureText}>{t('features.one.text')}</p>
            <div className={styles.featureImagePlaceholder}>
              <Image
                src="/assets/b2b/landing/feature-venues.webp"
                alt="Feature 1"
                width={500}
                height={500}
              />
            </div>
          </div>
          {/* Feature 2 */}
          <div className={styles.featureCard}>
            <h2 className={styles.featureTitle}>{t('features.two.title')}</h2>
            <p className={styles.featureText}>{t('features.two.text')}</p>
            <div className={styles.featureImagePlaceholder}>
              <Image
                src="/assets/b2b/landing/feature-discounts.webp"
                alt="Feature 1"
                width={500}
                height={500}
              />
            </div>
          </div>
          {/* Feature 3 */}
          <div className={styles.featureCard}>
            <h2 className={styles.featureTitle}>{t('features.three.title')}</h2>
            <p className={styles.featureText}>{t('features.three.text')}</p>
            <div className={styles.featureImagePlaceholder}>
              <Image
                src="/assets/b2b/landing/feature-loyalty.webp"
                alt="Feature 1"
                width={500}
                height={500}
              />
            </div>
          </div>
        </div>
      </section>

      {/* Contact Us Section */}
      <section id="contact-us" className={styles.contactSection}>
        <div className={`${styles.card} ${styles.contactCard}`}>
          <h2 className={styles.cardTitle}>{t('contactUs.title')}</h2>
          <p className={styles.cardDescription}>
            {t.rich('contactUs.description', {
              email: (chunks) => (
                <a
                  href="mailto:contacts@kegry.com"
                  className={styles.actionLink}
                >
                  {chunks}
                </a>
              ),
              chat: (chunks) => (
                <Link
                  href="#"
                  className={styles.actionLink}
                  onClick={(e) => {
                    e.preventDefault();
                    // @ts-ignore
                    if (window.Tawk_API) {
                      // @ts-ignore
                      window.Tawk_API.maximize();
                    }
                  }}
                >
                  {chunks}
                </Link>
              ),
            })}
          </p>
        </div>
        <div className={cn(styles.sideImage, '-mt-6')}>
          <Image
            src="/assets/landing/section-contact-us.webp"
            alt="Goblin Teacher"
            width={230}
            height={230}
          />
        </div>
      </section>

      {/* Footer */}
      <footer className={styles.footer}>
        <span className={styles.footerCopyright}>{t('footer.copyright')}</span>
        <SocialLinks />
        <div className={styles.footerLinks}>
          <Link href={PAGE_ROUTES.LEGAL.PRIVACY} className={styles.footerLink}>
            {t('footer.privacy')}
          </Link>
          <Link href={PAGE_ROUTES.LEGAL.CONSENT} className={styles.footerLink}>
            {t('footer.consent')}
          </Link>
          <Link href={PAGE_ROUTES.LEGAL.TERMS} className={styles.footerLink}>
            {t('footer.terms')}
          </Link>
          <Link
            href={PAGE_ROUTES.LEGAL.GUIDELINES}
            className={styles.footerLink}
          >
            {t('footer.community')}
          </Link>
        </div>
      </footer>
    </div>
  );
}
