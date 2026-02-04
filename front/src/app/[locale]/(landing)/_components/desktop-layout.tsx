import Image from 'next/image';
import Link from 'next/link';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from '../landing.module.css';

export default function DesktopLayout() {
  return (
    <div className={styles.wrapper}>
      <section className={styles.hero}>
        <div className={styles.logo}>
          <Image
            src="/assets/logo.png"
            alt="Hoppyfy Logo"
            width={120}
            height={120}
            priority
          />
        </div>
        <h1 className={styles.title}>Hoppyfy.me</h1>
        <p className={styles.subtitle}>The best stories start off-screen</p>
        <Link href={PAGE_ROUTES.LOGIN} className={styles.cta}>
          Get Started
        </Link>
      </section>

      <section className={styles.features}>
        <div className={styles.feature}>
          <div className={styles.featureIcon}>📍</div>
          <h3 className={styles.featureTitle}>Discover Nearby</h3>
          <p className={styles.featureDescription}>
            Find people around you who share your interests
          </p>
        </div>
        <div className={styles.feature}>
          <div className={styles.featureIcon}>🍺</div>
          <h3 className={styles.featureTitle}>Meet in Person</h3>
          <p className={styles.featureDescription}>
            Connect over drinks and real conversations
          </p>
        </div>
        <div className={styles.feature}>
          <div className={styles.featureIcon}>⭐</div>
          <h3 className={styles.featureTitle}>Build Connections</h3>
          <p className={styles.featureDescription}>
            Rate your experiences and grow your network
          </p>
        </div>
      </section>
    </div>
  );
}
