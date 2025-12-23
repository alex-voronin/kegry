'use client';

import Image from 'next/image';
import styles from './profile-image-section.module.css';
import { useRef, useState } from 'react';
import ProfileImageCropper from '../profile-image-cropper';

type ClickableImageProps = {
  onProfileImageChange?: (base64Avatar: string) => void;
};

export const ProfileImageSection = ({
  onProfileImageChange,
}: ClickableImageProps) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [imageToCropSrc, setImageToCropSrc] = useState<string | null>(null);
  const [profileImage, setProfileImage] = useState<string | null>(null);

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
    setProfileImage(base64Avatar);
    setImageToCropSrc(null);
    onProfileImageChange?.(base64Avatar);
  };

  if (imageToCropSrc)
    return (
      <ProfileImageCropper imageSrc={imageToCropSrc} onCrop={handleCrop} />
    );

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
          src={profileImage || '/assets/common/camera-placeholder.svg'}
          alt={'Profile Image'}
          width={192}
          height={192}
          className={styles.profileImage}
        />
      </div>
    </div>
  );
};
