import { useTranslations } from 'next-intl';
import Image from 'next/image';
import styles from './Logo.module.css';

interface LogoProps {
  b2b?: boolean;
  tagline?: string;
}

export function Logo({ b2b, tagline }: LogoProps) {
  const t = useTranslations('components.logo');
  const finalTagline = tagline || t('tagline');

  return (
    <div className="flex w-full justify-center">
      <div className={styles.header}>
        <Image
          src={b2b ? '/assets/b2b/logo.webp' : '/assets/common/logo.webp'}
          alt="Kegry"
          width={180}
          height={254}
          className={styles.logoImage}
          priority
        />
        <div className={styles.logoRow}>
          <span className={styles.brandName}>Kegry</span>
        </div>
        {finalTagline && <p className={styles.tagline}>{finalTagline}</p>}
      </div>
    </div>
  );
}
