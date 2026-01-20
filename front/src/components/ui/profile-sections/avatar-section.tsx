'use client';

import Image from 'next/image';
import styles from './avatar-section.module.css';
import { useRef, useState } from 'react';
import AvatarCropper from '../avatar-cropper';
import { blobToBase64 } from '@/store/use-user-profile-store';

type AvatarSectionProps = {
  avatarBase64?: string | null;
  onAvatarChange?: (avatarBase64: string) => void;
};

export const AvatarSection = ({
  avatarBase64,
  onAvatarChange,
}: AvatarSectionProps) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [imageToCropSrc, setImageToCropSrc] = useState<string | null>(null);
  const [localAvatarUrl, setLocalAvatarUrl] = useState<string | null>(null);

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

  const handleCrop = async (blob: Blob) => {
    // Revoke the source URL after cropping is complete
    if (imageToCropSrc) {
      URL.revokeObjectURL(imageToCropSrc);
    }

    // Convert blob to base64 for localStorage persistence
    const base64 = await blobToBase64(blob);
    setLocalAvatarUrl(base64);
    setImageToCropSrc(null);
    onAvatarChange?.(base64);
  };

  if (imageToCropSrc)
    return <AvatarCropper imageSrc={imageToCropSrc} onCrop={handleCrop} />;

  const displayUrl = localAvatarUrl || avatarBase64;

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
