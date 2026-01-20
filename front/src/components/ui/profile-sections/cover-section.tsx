'use client';

import Image from 'next/image';
import styles from './cover-section.module.css';
import { useRef, useState, useEffect, useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { compressImageToBlob } from '@/lib/image-utils';

type CoverSectionProps = {
  coverBlob?: Blob | null;
  onCoverChange?: (coverBlob: Blob) => void;
};

const MAX_COVER_SIZE_BYTES = 1024 * 1024; // 1MB
const MAX_COVER_WIDTH = 1920;

export const CoverSection = ({
  coverBlob,
  onCoverChange,
}: CoverSectionProps) => {
  const t = useTranslations('screens.onboarding');
  const inputRef = useRef<HTMLInputElement>(null);
  const [localBlob, setLocalBlob] = useState<Blob | null>(null);

  // Create object URL for display
  const displayUrl = useMemo(() => {
    const blob = localBlob || coverBlob;
    if (blob) {
      return URL.createObjectURL(blob);
    }
    return null;
  }, [localBlob, coverBlob]);

  // Cleanup object URL on unmount or when blob changes
  useEffect(() => {
    return () => {
      if (displayUrl) {
        URL.revokeObjectURL(displayUrl);
      }
    };
  }, [displayUrl]);

  const handleDivClick = () => {
    inputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];

      // Compress to under 1MB
      const compressed = await compressImageToBlob(
        file,
        MAX_COVER_SIZE_BYTES,
        MAX_COVER_WIDTH,
      );

      setLocalBlob(compressed);
      onCoverChange?.(compressed);
    }
  };

  return (
    <div className={styles.coverSection} onClick={handleDivClick}>
      <input
        type="file"
        accept="image/*"
        ref={inputRef}
        onChange={handleFileChange}
        className="hidden"
      />
      {displayUrl ? (
        <>
          <Image
            src={displayUrl}
            alt="Cover Image"
            fill
            className={styles.coverImage}
          />
          <div className={styles.overlay} />
        </>
      ) : (
        <div className={styles.placeholder}>
          <Image
            src="/assets/common/camera-placeholder.svg"
            alt="Select cover"
            width={64}
            height={64}
          />
          <span className={styles.placeholderText}>{t('selectCover')}</span>
        </div>
      )}
    </div>
  );
};
