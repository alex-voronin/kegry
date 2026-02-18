'use client';

import { useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import Script from 'next/script';
import { useTranslations } from 'next-intl';
import { PAGE_ROUTES } from '@/lib/routes';
import { useNewsStore } from '../store/use-news-store';
import SocialLinks from './social-links';
import styles from './desktop-landing.module.css';

export default function DesktopLayout() {
  const t = useTranslations('screens.landing');

  const scrollToSection = (sectionId: string) => {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth' });
    }
  };
  const { data: newsData, fetchNews } = useNewsStore();

  useEffect(() => {
    fetchNews();
  }, [fetchNews]);

  return (
    <div className={styles.wrapper}>
      {/* Fixed Header */}
      <header className={styles.header}>
        <div className={styles.logoContainer}>
          <Image
            src="/assets/landing/header-logo.webp"
            alt="H"
            width={38}
            height={64}
            className={styles.logoIconImage}
          />
          <div className={styles.logoTextWrapper}>
            <div className={styles.logoTitleRow}>
              <span className={styles.logoText}>icPlork this Inn</span>
              <Image
                src="/assets/landing/header-mug.webp"
                alt="Beer"
                width={20}
                height={20}
              />
            </div>
            <span className={styles.logoSubtitle}>{t('header.subtitle')}</span>
          </div>
        </div>
        <nav className={styles.nav}>
          <span
            className={styles.navLink}
            onClick={() => scrollToSection('polls-news')}
          >
            {t('header.nav.pollsNews')}
          </span>
          <span
            className={styles.navLink}
            onClick={() => scrollToSection('how-it-works')}
          >
            {t('header.nav.howItWorks')}
          </span>
          <span
            className={styles.navLink}
            onClick={() => scrollToSection('contact-us')}
          >
            {t('header.nav.contactUs')}
          </span>
          <Link href={PAGE_ROUTES.LOGIN(true)} className={styles.loginButton}>
            {t('header.nav.login')}
          </Link>
        </nav>
      </header>
      {/* Cheers Section */}
      <section className={styles.cheers}>
        <div className={styles.card}>
          <h1 className={styles.cardTitle}>{t('cheers.title')}</h1>
          <p className={styles.cardDescription}>
            {t.rich('cheers.description', {
              br: () => <br />,
            })}{' '}
            <Link href={PAGE_ROUTES.LOGIN(true)} className={styles.actionLink}>
              {t('cheers.cta')}
            </Link>
          </p>
        </div>

        <div className={styles.sideImage}>
          <Image
            src="/assets/landing/section-cheers.webp"
            alt="Cheers"
            width={400}
            height={400}
          />
        </div>
      </section>

      {/* Polls & News Section */}
      <section id="polls-news" className={styles.pollsNewsSection}>
        <div className={styles.sideImage}>
          <Image
            src="/assets/landing/section-news.webp"
            alt="News"
            width={360}
            height={360}
          />
        </div>

        <div className={`${styles.card} ${styles.newsCard}`}>
          <h2 className={styles.cardTitle}>{t('pollsNews.newsTitle')}</h2>
          <div className={styles.newsList}>
            {newsData &&
              newsData.map((news, index) => (
                <div key={index} className={styles.newsItem}>
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

        <div className={`${styles.card} ${styles.pollCard}`}>
          <h2 className={styles.cardTitle}>{t('pollsNews.pollTitle')}</h2>
          <p className={styles.cardDescription}>
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
      <section id="how-it-works" className={styles.howItWorksSection}>
        <div className={styles.card}>
          <h2 className={styles.cardTitle}>{t('howItWorks.title')}</h2>
          <div className={styles.howItWorksSteps}>
            <div className={styles.step}>
              <p className={styles.stepTitle}>{t('howItWorks.step1')}</p>
              <Image
                src="/assets/landing/step-1.webp"
                alt="Step 1"
                width={230}
                height={150}
                className={styles.stepImage}
              />
            </div>
            <div className={styles.step}>
              <p className={styles.stepTitle}>{t('howItWorks.step2')}</p>
              <Image
                src="/assets/landing/step-2.webp"
                alt="Step 2"
                width={180}
                height={130}
                className={styles.stepImage}
              />
            </div>
            <div className={styles.step}>
              <p className={styles.stepTitle}>{t('howItWorks.step3')}</p>
              <Image
                src="/assets/landing/step-3.webp"
                alt="Step 3"
                width={160}
                height={100}
                className={styles.stepImage}
              />
            </div>
          </div>
          <p className={styles.howItWorksNote}>{t('howItWorks.note')}</p>
        </div>
        <div className={styles.decorativeImageWrapper}>
          <Image
            src="/assets/landing/section-how-it-works.webp"
            alt="Dynamo"
            width={450}
            height={450}
            className={styles.decorativeImage}
          />
        </div>
      </section>

      {/* Contact Us Section */}
      <section id="contact-us" className={styles.contactSection}>
        <div className={styles.sideImage}>
          <Image
            src="/assets/landing/section-contact-us.webp"
            alt="Goblin Artist"
            width={300}
            height={400}
          />
        </div>

        <div className={`${styles.card} ${styles.contactCard}`}>
          <h2 className={styles.cardTitle}>{t('contactUs.title')}</h2>
          <p className={styles.cardDescription}>
            {t.rich('contactUs.description', {
              email: (chunks) => (
                <a
                  href="mailto:contacts@hicplork.com"
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
