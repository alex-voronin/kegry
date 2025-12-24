'use client';

import Image from 'next/image';
import styles from './avatar-section.module.css';
import { useRef, useState } from 'react';
import AvatarCropper from '../avatar-cropper';

type ClickableImageProps = {
  src?: string;
  onAvatarChange?: (base64Avatar: string) => void;
};

export const AvatarSection = ({
  src = '/assets/common/camera-placeholder.svg',
  onAvatarChange,
}: ClickableImageProps) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [imageToCropSrc, setImageToCropSrc] = useState<string | null>(null);
  const [avatarImage, setAvatarImage] = useState<string | null>(null);

  const handleDivClick = () => {
    // hack to open file picker
    inputRef.current?.click();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      setImageToCropSrc(URL.createObjectURL(file));
    }
  };

  const handleCrop = (base64Avatar: string) => {
    setAvatarImage(base64Avatar);
    setImageToCropSrc(null);
    onAvatarChange?.(base64Avatar);
  };

  if (imageToCropSrc)
    return <AvatarCropper imageSrc={imageToCropSrc} onCrop={handleCrop} />;

  return (
    <div className={styles.profileSection} onClick={handleDivClick}>
      <div className={styles.profilePlaceholder}>
        <input
          type="file"
          accept="image/*"
          ref={inputRef}
          onChange={handleFileChange}
          className="hidden"
        />
        <Image
          src={avatarImage || src}
          alt={'Avatar Image'}
          width={192}
          height={192}
          className={styles.avatarImage}
        />
      </div>
    </div>
  );
};
