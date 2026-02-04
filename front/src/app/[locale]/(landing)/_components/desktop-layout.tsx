'use client';

import Link from 'next/link';
import Image from 'next/image';
import Script from 'next/script';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../landing.module.css';

export default function DesktopLayout() {
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
            src="/assets/landing/header-logo.webp"
            alt="H"
            width={38}
            height={64}
            className={styles.logoIconImage}
          />
          <div className={styles.logoTextWrapper}>
            <div className={styles.logoTitleRow}>
              <span className={styles.logoText}>oplingsHere</span>
              <Image
                src="/assets/landing/header-mug.webp"
                alt="Beer"
                width={20}
                height={20}
              />
            </div>
            <span className={styles.logoSubtitle}>
              The best stories start off-screen
            </span>
          </div>
        </div>
        <nav className={styles.nav}>
          <span
            className={styles.navLink}
            onClick={() => scrollToSection('polls-news')}
          >
            Polls & News
          </span>
          <span
            className={styles.navLink}
            onClick={() => scrollToSection('how-it-works')}
          >
            How It Works
          </span>
          <span
            className={styles.navLink}
            onClick={() => scrollToSection('contact-us')}
          >
            Contact Us
          </span>
          <Link href={PAGE_ROUTES.LOGIN} className={styles.loginButton}>
            Log In/Sign Up
          </Link>
        </nav>
      </header>
      {/* Hero Section */}
      <section className={styles.hero}>
        <div className={styles.card}>
          <h1 className={styles.cardTitle}>Find your bar buddy right now!</h1>
          <p className={styles.cardDescription}>
            Up for a drink and good company? No dating. No endless chats.
            <br />
            Check a profile, send an invite, and meet at a bar. Talk about life,
            laugh, vent and keep your head up. If any hoplings appear, maybe
            it&apos;s time to slow down a bit 🍺
          </p>
        </div>
        <div className={styles.heroImage}>
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
        <div className={styles.goblinImage}>
          <Image
            src="/assets/landing/section-news.webp"
            alt="News"
            width={360}
            height={360}
          />
        </div>

        {/* News Card */}
        <div className={`${styles.card} ${styles.newsCard}`}>
          <h2 className={styles.cardTitle}>News</h2>
          <div className={styles.newsItem}>
            <span className={styles.newsDate}>20/02/2022</span>
            <span>We&apos;ve started open beta! 🎉</span>
          </div>
          <div className={styles.newsItem}>
            <span className={styles.newsDate}>???</span>
            <span>What&apos;s next? 🤔</span>
          </div>
        </div>

        {/* Poll Card */}
        <div className={`${styles.card} ${styles.pollCard}`}>
          <iframe
            data-tally-src="https://tally.so/embed/obGVDN?alignLeft=1&hideTitle=0&transparentBackground=1&dynamicHeight=1"
            loading="lazy"
            width="100%"
            height="100%"
            frameBorder="0"
            title="Poll"
            className="h-full w-full"
          ></iframe>
          <Script
            src="https://tally.so/widgets/embed.js"
            strategy="lazyOnload"
            onLoad={() => {
              // @ts-ignore
              if (typeof Tally !== 'undefined') {
                // @ts-ignore
                Tally.loadEmbeds();
              }
            }}
          />
        </div>
      </section>

      {/* How It Works Section */}
      <section id="how-it-works" className={styles.howItWorksSection}>
        <div className={styles.card}>
          <h2 className={styles.cardTitle}>How It Works</h2>
          <div className={styles.howItWorksSteps}>
            <div className={styles.step}>
              <p className={styles.stepTitle}>
                1. Browse users and invite them
              </p>
              <Image
                src="/assets/landing/step-1.webp"
                alt="Step 1"
                width={230}
                height={150}
                className={styles.stepImage}
              />
            </div>
            <div className={styles.step}>
              <p className={styles.stepTitle}>2. Chat and plan</p>
              <Image
                src="/assets/landing/step-2.webp"
                alt="Step 2"
                width={180}
                height={130}
                className={styles.stepImage}
              />
            </div>
            <div className={styles.step}>
              <p className={styles.stepTitle}>3. Meet and enjoy</p>
              <Image
                src="/assets/landing/step-3.webp"
                alt="Step 3"
                width={160}
                height={100}
                className={styles.stepImage}
              />
            </div>
          </div>
          <p className={styles.howItWorksNote}>
            Hey adventurer! Just a heads-up: invites expire in 48 hours. You can
            have only one chat at a time and 30 messages per chat, so choose
            wisely. Invite buddies only if you&apos;re really ready for a bar
            adventure and hurry before the hoplings drink all the beer!
          </p>
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
        <div className={styles.contactImage}>
          <Image
            src="/assets/landing/section-contact-us.webp"
            alt="Goblin Artist"
            width={300}
            height={400}
          />
        </div>

        <div className={`${styles.card} ${styles.contactCard}`}>
          <h2 className={styles.cardTitle}>Contact Us</h2>
          <p className={styles.cardDescription}>
            You might be wondering the same thing we are: are these hoplings the
            same or different? Honestly, we&apos;re not sure yet... We still
            haven&apos;t figured out how to tell them apart. If you happen to
            know how to, or if you have any questions or suggestions about the
            service, we&apos;d be happy to hear from you via email at{' '}
            <a
              href="mailto:contacts@hoplingshere.com"
              className={styles.contactLink}
            >
              contacts@hoplingshere.com
            </a>{' '}
            or through our{' '}
            <Link
              href="#"
              className={styles.contactLink}
              onClick={(e) => {
                e.preventDefault();
                // @ts-ignore
                if (window.Tawk_API) {
                  // @ts-ignore
                  window.Tawk_API.maximize();
                }
              }}
            >
              chat
            </Link>
            .
          </p>
        </div>
      </section>

      {/* Footer */}
      <footer className={styles.footer}>
        <span className={styles.footerCopyright}>
          © 2026 HoplingsHere™. All rights reserved.
        </span>
        <div className={styles.footerLinks}>
          <Link href="#" className={styles.footerLink}>
            Privacy Policy
          </Link>
          <Link href="#" className={styles.footerLink}>
            Cookie & Analytics Policy
          </Link>
          <Link href="#" className={styles.footerLink}>
            Terms of Service
          </Link>
          <Link href="#" className={styles.footerLink}>
            Community Guidelines
          </Link>
        </div>
      </footer>
    </div>
  );
}
