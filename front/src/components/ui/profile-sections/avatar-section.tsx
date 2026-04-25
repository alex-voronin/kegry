'use client';

import styles from './avatar-section.module.css';
import { useRef, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Camera } from 'lucide-react';
import { ImageCropperModal } from '../image-cropper-modal';
import { blobToBase64 } from '@/store/use-user-profile-store';
import { AVATAR_MAX_LONG_SIDE } from '@/lib/constants';

type AvatarSectionProps = {
  avatarBase64?: string | null;
  onAvatarChange?: (avatarBase64: string) => void;
};

export const AvatarSection = ({
  avatarBase64,
  onAvatarChange,
}: AvatarSectionProps) => {
  const t = useTranslations('components.profile-sections.avatar');
  const inputRef = useRef<HTMLInputElement>(null);
  const [imageToCropSrc, setImageToCropSrc] = useState<string | null>(null);
  const [localAvatarUrl, setLocalAvatarUrl] = useState<string | null>(null);

  const handleClick = () => {
    // Pass click to invisible file input
    inputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      try {
        const base64 = await blobToBase64(file);
        setImageToCropSrc(base64);
      } catch (error) {
        console.error('Failed to read file:', error);
      }
    }
    // Reset input so same file can be selected again
    e.target.value = '';
  };

  const handleCrop = async (blob: Blob) => {
    // Convert blob to base64 for localStorage persistence
    const base64 = await blobToBase64(blob);
    setLocalAvatarUrl(base64);
    setImageToCropSrc(null);
    onAvatarChange?.(base64);
  };

  const displayUrl = localAvatarUrl || avatarBase64;

  return (
    <div className={styles.avatarSection} onClick={handleClick}>
      <div className={styles.avatarContainer}>
        {displayUrl ? (
          <img src={displayUrl} alt="avatar" className={styles.avatarImage} />
        ) : (
          <div className={styles.avatarPlaceholderIcon}>
            <Camera size={40} className="text-secondary opacity-50" />
            <span className={styles.requiredAsterisk}>*</span>
          </div>
        )}
        <input
          type="file"
          accept="image/*"
          ref={inputRef}
          onChange={handleFileChange}
          className="hidden"
        />
      </div>

      <ImageCropperModal
        isOpen={!!imageToCropSrc}
        onClose={() => setImageToCropSrc(null)}
        imageSrc={imageToCropSrc}
        onCropComplete={handleCrop}
        cropShape="round"
        outputSize={{ width: AVATAR_MAX_LONG_SIDE, height: AVATAR_MAX_LONG_SIDE }}
      />
    </div>
  );
};
