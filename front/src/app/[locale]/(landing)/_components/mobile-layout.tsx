'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import Script from 'next/script';
import { useTranslations } from 'next-intl';
import { Menu, X } from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';
import { useNewsStore } from '../store/use-news-store';
import styles from './desktop-landing.module.css';
import mobileStyles from './mobile-landing.module.css';

export default function MobileLayout() {
  const t = useTranslations('screens.landing');
  const { data: newsData, fetchNews } = useNewsStore();
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  useEffect(() => {
    fetchNews();
  }, [fetchNews]);

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
            src="/assets/landing/header-logo.webp"
            alt="H"
            width={28}
            height={60}
            className={`${styles.logoIconImage} ${mobileStyles.mobileLogoImage}`}
          />
          <div className={mobileStyles.mobileLogoTextWrapper}>
            <div className={mobileStyles.mobileLogoTitleRow}>
              <span className={mobileStyles.mobileLogoText}>oplingsHere</span>
              <Image
                src="/assets/landing/header-mug.webp"
                alt="Beer"
                width={20}
                height={29}
              />
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
        <button
          className={mobileStyles.drawerCloseButton}
          onClick={() => setIsMenuOpen(false)}
          aria-label="Close menu"
        >
          <X size={28} />
        </button>

        <nav className={mobileStyles.drawerNav}>
          <span
            className={mobileStyles.drawerNavLink}
            onClick={() => scrollToSection('polls-news')}
          >
            {t('header.nav.pollsNews')}
          </span>
          <span
            className={mobileStyles.drawerNavLink}
            onClick={() => scrollToSection('how-it-works')}
          >
            {t('header.nav.howItWorks')}
          </span>
          <span
            className={mobileStyles.drawerNavLink}
            onClick={() => scrollToSection('contact-us')}
          >
            {t('header.nav.contactUs')}
          </span>
        </nav>

        <Link
          href={PAGE_ROUTES.LOGIN(true)}
          className={mobileStyles.drawerSignUpButton}
          onClick={() => setIsMenuOpen(false)}
        >
          {t('header.nav.login')}
        </Link>
        <div className={mobileStyles.drawerLegalLinks}>
          <Link
            href={PAGE_ROUTES.LEGAL.PRIVACY}
            className={mobileStyles.drawerLegalLink}
          >
            {t('footer.privacy')}
          </Link>
          <Link
            href={PAGE_ROUTES.LEGAL.TERMS}
            className={mobileStyles.drawerLegalLink}
          >
            {t('footer.terms')}
          </Link>
          <Link
            href={PAGE_ROUTES.LEGAL.CONSENT}
            className={mobileStyles.drawerLegalLink}
          >
            {t('footer.consent')}
          </Link>
          <Link
            href={PAGE_ROUTES.LEGAL.GUIDELINES}
            className={mobileStyles.drawerLegalLink}
          >
            {t('footer.community')}
          </Link>
        </div>
      </aside>

      {/* Cheers Section */}
      <section className={mobileStyles.mobileSection}>
        <div className={mobileStyles.mobileSectionImage}>
          <Image
            src="/assets/landing/section-cheers.webp"
            alt="Cheers"
            width={280}
            height={280}
            className={mobileStyles.sectionImg}
          />
        </div>
        <div className={`${styles.card} ${mobileStyles.mobileSectionCard}`}>
          <h1 className={mobileStyles.mobileCardTitle}>{t('cheers.title')}</h1>
          <p className={mobileStyles.mobileCardDescription}>
            {t.rich('cheers.description', {
              br: () => <br />,
            })}{' '}
            <Link href={PAGE_ROUTES.LOGIN(true)} className={styles.actionLink}>
              {t('cheers.cta')}
            </Link>
          </p>
        </div>
      </section>

      {/* Polls & News Section */}
      <section id="mobile-polls-news" className={mobileStyles.mobileSection}>
        <div className={mobileStyles.mobileSectionImage}>
          <Image
            src="/assets/landing/section-news.webp"
            alt="News"
            width={260}
            height={214}
            className={mobileStyles.sectionImg}
          />
        </div>
        <div className={`${styles.card} ${mobileStyles.mobileSectionCard}`}>
          <h2 className={mobileStyles.mobileCardTitle}>
            {t('pollsNews.newsTitle')}
          </h2>
          <div className={mobileStyles.mobileNewsList}>
            {newsData &&
              newsData.map((news, index) => (
                <div key={index} className={mobileStyles.mobileNewsItem}>
                  <span className={styles.newsDate}>
                    {new Date(news.date).toLocaleDateString(undefined, {
                      year: 'numeric',
                      month: '2-digit',
                      day: '2-digit',
                    })}
                  </span>
                  <span>{news.content}</span>
                </div>
              ))}
          </div>
        </div>
        <div className={`${styles.card} ${mobileStyles.mobilePollCard}`}>
          <h2 className={styles.cardTitle}>{t('pollsNews.pollTitle')}</h2>
          <p className={mobileStyles.mobileCardDescription}>
            {t('pollsNews.pollDescription')}
          </p>
          <a
            href="https://tally.so/r/obGVDN"
            target="_blank"
            rel="noopener noreferrer"
            className={styles.pollButton}
          >
            {t('pollsNews.pollButton')}
          </a>
        </div>
      </section>

      {/* How It Works Section */}
      <section id="mobile-how-it-works" className={mobileStyles.mobileSection}>
        <div className={mobileStyles.mobileSectionImage}>
          <Image
            src="/assets/landing/section-how-it-works.webp"
            alt="How It Works"
            width={280}
            height={280}
            className={mobileStyles.sectionImg}
          />
        </div>
        <div className={`${styles.card} ${mobileStyles.mobileSectionCard}`}>
          <h2 className={mobileStyles.mobileCardTitle}>
            {t('howItWorks.title')}
          </h2>
          <div className={mobileStyles.mobileSteps}>
            <div className={mobileStyles.mobileStep}>
              <p className={mobileStyles.mobileStepTitle}>
                {t('howItWorks.step1')}
              </p>
              <Image
                src="/assets/landing/step-1.webp"
                alt="Step 1"
                width={200}
                height={100}
                className={mobileStyles.mobileStepImage}
              />
            </div>
            <div className={mobileStyles.mobileStep}>
              <p className={mobileStyles.mobileStepTitle}>
                {t('howItWorks.step2')}
              </p>
              <Image
                src="/assets/landing/step-2.webp"
                alt="Step 2"
                width={200}
                height={90}
                className={mobileStyles.mobileStepImage}
              />
            </div>
            <div className={mobileStyles.mobileStep}>
              <p className={mobileStyles.mobileStepTitle}>
                {t('howItWorks.step3')}
              </p>
              <Image
                src="/assets/landing/step-3.webp"
                alt="Step 3"
                width={160}
                height={80}
                className={mobileStyles.mobileStepImage}
              />
            </div>
          </div>
          <p className={mobileStyles.mobileCardDescription}>
            {t('howItWorks.note')}
          </p>
        </div>
      </section>

      {/* Contact Us Section */}
      <section id="mobile-contact-us" className={mobileStyles.mobileSection}>
        <div className={mobileStyles.mobileSectionImage}>
          <Image
            src="/assets/landing/section-contact-us.webp"
            alt="Contact Us"
            width={220}
            height={300}
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
                  href="mailto:contacts@hoplingshere.com"
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
