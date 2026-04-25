'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { Menu, X } from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';
import { LanguageSelector } from '@/components/common/LanguageSelector';
import { ScrollToTopButton } from '@/components/common/ScrollToTopButton';
import SocialLinks from './social-links';
import styles from './desktop-landing.module.css';
import mobileStyles from './mobile-landing.module.css';

export default function MobileLayout() {
  const t = useTranslations('screens.landing');
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const scrollToSection = (sectionId: string) => {
    setIsMenuOpen(false);
    setTimeout(() => {
      const element = document.getElementById(`mobile-${sectionId}`);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
      }
    }, 300); // Wait for drawer to close
  };

  return (
    <div className={styles.wrapper}>
      {/* Header */}
      <header className={mobileStyles.mobileHeader}>
        <div className={mobileStyles.mobileLogoContainer}>
          <Image
            src="/assets/b2b/landing/header-logo.webp"
            alt="Kegry"
            width={46}
            height={64}
            className={`${styles.logoIconImage} ${mobileStyles.mobileLogoImage}`}
          />
          <div className={mobileStyles.mobileLogoTextWrapper}>
            <div className={mobileStyles.mobileLogoTitleRow}>
              <span className={mobileStyles.mobileLogoText}>
                {t('header.brandName')}
              </span>
            </div>
            <span className={mobileStyles.mobileLogoSubtitle}>
              {t('header.subtitle')}
            </span>
          </div>
        </div>
        <button
          className={mobileStyles.burgerButton}
          onClick={() => setIsMenuOpen(true)}
          aria-label="Open menu"
        >
          <Menu size={28} />
        </button>
      </header>

      {/* Side Menu Drawer */}
      <div
        className={`${mobileStyles.drawerOverlay} ${isMenuOpen ? mobileStyles.drawerOverlayOpen : ''}`}
        onClick={() => setIsMenuOpen(false)}
      />
      <aside
        className={`${mobileStyles.drawer} ${isMenuOpen ? mobileStyles.drawerOpen : ''}`}
      >
        <div className={mobileStyles.drawerHeader}>
          <LanguageSelector />
          <button
            className={mobileStyles.drawerCloseButton}
            onClick={() => setIsMenuOpen(false)}
            aria-label="Close menu"
          >
            <X size={28} />
          </button>
        </div>

        <nav className={mobileStyles.drawerNav}>
          <span
            className={mobileStyles.drawerNavLink}
            onClick={() => {
              const hostname = window.location.hostname;
              const newHostname = hostname.startsWith('business.')
                ? hostname.replace('business.', '')
                : hostname;
              window.location.href =
                window.location.protocol +
                '//' +
                newHostname +
                window.location.pathname;
            }}
          >
            {t('header.nav.forCustomers')}
          </span>
          <span
            className={mobileStyles.drawerNavLink}
            onClick={() => scrollToSection('features')}
          >
            {t('header.nav.features')}
          </span>
          <span
            className={mobileStyles.drawerNavLink}
            onClick={() => scrollToSection('contact-us')}
          >
            {t('header.nav.contactUs')}
          </span>
        </nav>

        <Link
          href={PAGE_ROUTES.B2B.LOGIN(true)}
          className={mobileStyles.drawerSignUpButton}
          onClick={() => setIsMenuOpen(false)}
        >
          {t('header.login')}
        </Link>
        <div className={mobileStyles.drawerSocialLinks}>
          <SocialLinks />
        </div>
        <div className={mobileStyles.drawerLegalLinks}>
          <Link
            href={PAGE_ROUTES.LEGAL.PRIVACY}
            className={mobileStyles.drawerLegalLink}
          >
            {t('footer.privacy')}
          </Link>
          <Link
            href={PAGE_ROUTES.LEGAL.CONSENT}
            className={mobileStyles.drawerLegalLink}
          >
            {t('footer.consent')}
          </Link>
          <Link
            href={PAGE_ROUTES.LEGAL.TERMS}
            className={mobileStyles.drawerLegalLink}
          >
            {t('footer.terms')}
          </Link>
          <Link
            href={PAGE_ROUTES.LEGAL.GUIDELINES}
            className={mobileStyles.drawerLegalLink}
          >
            {t('footer.community')}
          </Link>
        </div>
      </aside>

      {/* Hero Section */}
      <section
        className={`${mobileStyles.mobileSection} ${mobileStyles.mobileHeroSection}`}
      >
        <div className={mobileStyles.mobileSectionImage}>
          <Image
            src="/assets/b2b/landing/section-hero.webp"
            alt="Corn Graph"
            width={280}
            height={278}
            className={mobileStyles.sectionImg}
          />
        </div>
        <div className={`${styles.card} ${mobileStyles.mobileSectionCard}`}>
          <h1 className={mobileStyles.mobileCardTitle}>{t('hero.title')}</h1>
          <p className={mobileStyles.mobileCardDescription}>
            {t('hero.description')}
          </p>
          <div className={mobileStyles.mobileLoginButtonWrapper}>
            <Link
              href={PAGE_ROUTES.B2B.LOGIN(true)}
              className={styles.loginButton}
            >
              {t('header.login')}
            </Link>
          </div>
        </div>
      </section>

      {/* Features Grid Section */}
      <section
        id="mobile-features"
        className={mobileStyles.mobileFeaturesSection}
      >
        <div className={mobileStyles.mobileFeaturesGrid}>
          {/* Feature 1 */}
          <div className={mobileStyles.mobileFeatureCard}>
            <h2 className={mobileStyles.mobileFeatureTitle}>
              {t('features.one.title')}
            </h2>
            <p className={mobileStyles.mobileFeatureText}>
              {t('features.one.text')}
            </p>
            <div className={mobileStyles.mobileFeatureImagePlaceholder}>
              <Image
                src="/assets/b2b/landing/feature-venues.webp"
                alt="Feature 1"
                width={500}
                height={156}
              />
            </div>
          </div>
          {/* Feature 2 */}
          <div className={mobileStyles.mobileFeatureCard}>
            <h2 className={mobileStyles.mobileFeatureTitle}>
              {t('features.two.title')}
            </h2>
            <p className={mobileStyles.mobileFeatureText}>
              {t('features.two.text')}
            </p>
            <div className={mobileStyles.mobileFeatureImagePlaceholder}>
              <Image
                src="/assets/b2b/landing/feature-discounts.webp"
                alt="Feature 2"
                width={500}
                height={160}
              />
            </div>
          </div>
          {/* Feature 3 */}
          <div className={mobileStyles.mobileFeatureCard}>
            <h2 className={mobileStyles.mobileFeatureTitle}>
              {t('features.three.title')}
            </h2>
            <p className={mobileStyles.mobileFeatureText}>
              {t('features.three.text')}
            </p>
            <div className={mobileStyles.mobileFeatureImagePlaceholder}>
              <Image
                src="/assets/b2b/landing/feature-loyalty.webp"
                alt="Feature 3"
                width={500}
                height={114}
              />
            </div>
          </div>
        </div>
      </section>

      {/* Contact Us Section */}
      <section id="mobile-contact-us" className={mobileStyles.mobileSection}>
        <div className={mobileStyles.mobileSectionImage}>
          <Image
            src="/assets/b2b/landing/section-contact-us.webp"
            alt="Contact Us"
            width={300}
            height={282}
            className={mobileStyles.sectionImg}
          />
        </div>
        <div className={`${styles.card} ${mobileStyles.mobileSectionCard}`}>
          <h2 className={mobileStyles.mobileCardTitle}>
            {t('contactUs.title')}
          </h2>
          <p className={mobileStyles.mobileCardDescription}>
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
      </section>

      {/* Footer */}
      <footer className={mobileStyles.mobileFooter}>
        <SocialLinks />
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
        <span className={styles.footerCopyright}>{t('footer.copyright')}</span>
      </footer>

      <ScrollToTopButton containerId="landing-scroll-container" />
    </div>
  );
}
