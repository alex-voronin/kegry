'use client';

import Image from 'next/image';
import styles from './profile-image-section.module.css';

type ClickableImageProps = {
  src: string;
  onClick?: () => void;
};

export const ProfileImageSection = ({ src, onClick }: ClickableImageProps) => {
  return (
    <div className={styles.profileSection} onClick={onClick}>
      <div className={styles.profilePlaceholder}>
        <Image
          src={src}
          alt={'Profile Image'}
          width={64}
          height={64}
          className={styles.profileImage}
        />
      </div>
    </div>
  );
};
