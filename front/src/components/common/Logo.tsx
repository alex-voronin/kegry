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
        <div className={styles.logoRow}>
          <Image
            src="/assets/common/logo.webp"
            alt="H"
            width={180}
            height={254}
            className={styles.logoImage}
            priority
          />
          <span className={styles.brandName}>icPlork</span>
          <Image
            src="/assets/common/beer-mug.webp"
            alt="Beer"
            width={32}
            height={45}
            className={styles.beerMug}
          />
        </div>
        {finalTagline && <p className={styles.tagline}>{finalTagline}</p>}
      </div>
    </div>
  );
}
