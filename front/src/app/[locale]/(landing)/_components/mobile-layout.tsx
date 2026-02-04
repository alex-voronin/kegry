'use client';

import Link from 'next/link';
import Image from 'next/image';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../landing.module.css';

export default function MobileLayout() {
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
          Log In
        </Link>
      </header>

      {/* Hero Section */}
      <section className={`${styles.hero} ${styles.mobileHero}`}>
        <div className={styles.card}>
          <h1 className={styles.cardTitle}>Find your bar buddy right now!</h1>
          <p className={styles.cardDescription}>
            Up for a drink and good company? No dating. No endless chats. Check
            a profile, send an invite, and meet at a bar. 🍺
          </p>
        </div>
        <div className={styles.heroImage}>
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
        className={`${styles.howItWorksSection} ${styles.mobileHowItWorks}`}
      >
        <div className={styles.card}>
          <h2 className={styles.cardTitle}>How It Works</h2>
          <div className={styles.mobileSteps}>
            <div className={styles.mobileStep}>
              <p className={styles.stepTitle}>1. Browse & Invite</p>
              <Image
                src="/assets/landing/step-1.webp"
                alt="Step 1"
                width={150}
                height={100}
                className={styles.stepImagePlaceholder}
              />
            </div>
            <div className={styles.mobileStep}>
              <p className={styles.stepTitle}>2. Chat & Plan</p>
              <Image
                src="/assets/landing/step-2.webp"
                alt="Step 2"
                width={150}
                height={100}
                className={styles.stepImagePlaceholder}
              />
            </div>
            <div className={styles.mobileStep}>
              <p className={styles.stepTitle}>3. Meet & Enjoy</p>
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
      <footer className={`${styles.footer} ${styles.mobileFooter}`}>
        <span className={styles.footerCopyright}>© 2026 HoplingsHere™</span>
        <div className={styles.mobileFooterLinks}>
          <Link href="#" className={styles.footerLink}>
            Privacy
          </Link>
          <Link href="#" className={styles.footerLink}>
            Terms
          </Link>
        </div>
      </footer>
    </div>
  );
}
