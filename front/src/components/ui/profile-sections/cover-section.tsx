'use client';

import Image from 'next/image';
import styles from './cover-section.module.css';
import { useRef, useState, useEffect, useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { Camera } from 'lucide-react';
import { compressImageToBlob } from '@/lib/image-utils';
import { toast } from 'react-toastify';
import {
  USER_COVER_MAX_LONG_SIDE,
  USER_COVER_MAX_SIZE_BYTES,
} from '@/lib/constants';
import { Spinner } from '../spinner';

type CoverSectionProps = {
  coverBlob?: Blob | null;
  onCoverChange?: (coverBlob: Blob) => void;
  existingCoverUrl?: string;
};

export const CoverSection = ({
  coverBlob,
  onCoverChange,
  existingCoverUrl,
}: CoverSectionProps) => {
  const t = useTranslations('screens.onboarding');
  const inputRef = useRef<HTMLInputElement>(null);
  const [localBlob, setLocalBlob] = useState<Blob | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  // Create object URL for display
  const displayUrl = useMemo(() => {
    const blob = localBlob || coverBlob;
    if (blob) {
      return URL.createObjectURL(blob);
    }
    return existingCoverUrl || null;
  }, [localBlob, coverBlob, existingCoverUrl]);

  // Cleanup object URL on unmount or when blob changes
  useEffect(() => {
    return () => {
      if (displayUrl && displayUrl.startsWith('blob:')) {
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
      setIsProcessing(true);
      try {
        const compressed = await compressImageToBlob(
          file,
          USER_COVER_MAX_SIZE_BYTES,
          USER_COVER_MAX_LONG_SIDE,
        );

        setLocalBlob(compressed);
        onCoverChange?.(compressed);
      } catch (error) {
        console.error('Failed to process cover image:', error);
        toast.error(t('uploadFailed'));
      } finally {
        setIsProcessing(false);
      }
    }
    // Reset input so same file can be selected again
    e.target.value = '';
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
          <Camera size={48} className="text-secondary opacity-50" />
          <span className={styles.placeholderText}>{t('selectCover')}</span>
        </div>
      )}
      {isProcessing && (
        <div className={styles.loadingOverlay}>
          <Spinner />
        </div>
      )}
    </div>
  );
};
