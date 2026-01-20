'use client';

import Image from 'next/image';
import styles from './avatar-section.module.css';
import { useRef, useState, useMemo, useEffect } from 'react';
import AvatarCropper from '../avatar-cropper';

type AvatarSectionProps = {
  avatarBlob?: Blob | null;
  onAvatarChange?: (avatarBlob: Blob) => void;
};

export const AvatarSection = ({
  avatarBlob,
  onAvatarChange,
}: AvatarSectionProps) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [imageToCropSrc, setImageToCropSrc] = useState<string | null>(null);
  const [localBlob, setLocalBlob] = useState<Blob | null>(null);

  const displayUrl = useMemo(() => {
    const blob = localBlob || avatarBlob;
    if (blob) {
      return URL.createObjectURL(blob);
    }
    return null;
  }, [localBlob, avatarBlob]);

  // Cleanup object URL on unmount or when blob changes
  useEffect(() => {
    return () => {
      if (displayUrl) {
        URL.revokeObjectURL(displayUrl);
      }
    };
  }, [displayUrl]);

  const handleClick = () => {
    // Pass click to invisible file input
    inputRef.current?.click();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      setImageToCropSrc(URL.createObjectURL(file));
    }
  };

  const handleCrop = (blob: Blob) => {
    // Revoke the source URL after cropping is complete
    if (imageToCropSrc) {
      URL.revokeObjectURL(imageToCropSrc);
    }
    setLocalBlob(blob);
    setImageToCropSrc(null);
    onAvatarChange?.(blob);
  };

  if (imageToCropSrc)
    return <AvatarCropper imageSrc={imageToCropSrc} onCrop={handleCrop} />;

  return (
    <div className={styles.profileSection} onClick={handleClick}>
      <div className={styles.profilePlaceholder}>
        <input
          type="file"
          accept="image/*"
          ref={inputRef}
          onChange={handleFileChange}
          className="hidden"
        />
        <Image
          src={displayUrl || '/assets/common/camera-placeholder.svg'}
          alt={'Avatar Image'}
          width={192}
          height={192}
          className={styles.avatarImage}
        />
      </div>
    </div>
  );
};
