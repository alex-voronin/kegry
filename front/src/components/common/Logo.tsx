import { useTranslations } from 'next-intl';
import Image from 'next/image';
import styles from './Logo.module.css';

interface LogoProps {
  tagline?: string;
}

export function Logo({ tagline }: LogoProps) {
  const t = useTranslations('components.logo');
  const finalTagline = tagline || t('tagline');

  return (
    <div className="flex w-full justify-center">
      <div className={styles.header}>
        <Image
          src="/assets/common/logo.webp"
          alt="Wibblycorn"
          width={180}
          height={254}
          className={styles.logoImage}
          priority
        />
        <div className={styles.logoRow}>
          <span className={styles.brandName}>Wibblycorn</span>
        </div>
        {finalTagline && <p className={styles.tagline}>{finalTagline}</p>}
      </div>
    </div>
  );
}
