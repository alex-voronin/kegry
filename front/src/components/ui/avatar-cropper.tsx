'use client';

import { useState, useCallback, useEffect, useRef } from 'react';
import { useTranslations } from 'next-intl';
import Cropper from 'react-easy-crop';
import { Button } from './button';
import { getCroppedImg } from '@/lib/image-utils';

export default function AvatarCropper({
  imageSrc,
  onCrop,
  onClose,
}: {
  imageSrc: string;
  onCrop: (avatarBlob: Blob) => void;
  onClose?: () => void;
}) {
  const t = useTranslations('components.avatar-cropper');
  const [crop, setCrop] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<any>(null);
  const imageSrcRef = useRef(imageSrc);

  const onCropComplete = useCallback((_: any, croppedAreaPixels: any) => {
    setCroppedAreaPixels(croppedAreaPixels);
  }, []);

  const saveCroppedImage = useCallback(async () => {
    if (!croppedAreaPixels) return;
    const blob = await getCroppedImg(imageSrc, croppedAreaPixels, undefined, {
      width: 256,
      height: 256,
    });
    if (blob) onCrop(blob);
  }, [croppedAreaPixels, imageSrc, onCrop]);

  return (
    <div className="fixed inset-0 z-50 flex h-full w-full flex-col bg-black">
      {onClose && (
        <button
          onClick={onClose}
          className="absolute top-4 left-4 z-50 text-2xl text-white"
        >
          ✕
        </button>
      )}
      <div className="relative flex-1">
        <Cropper
          image={imageSrc}
          crop={crop}
          zoom={zoom}
          aspect={1}
          cropShape="round"
          showGrid={false}
          onCropChange={setCrop}
          onZoomChange={setZoom}
          onCropComplete={onCropComplete}
        />
      </div>

      <div className="fixed bottom-8 flex w-full items-center justify-center gap-4 px-4">
        <input
          type="range"
          min={1}
          max={3}
          step={0.01}
          value={zoom}
          onChange={(e) => setZoom(Number(e.target.value))}
          className="accent-primary bg-secondary h-2 w-40 rounded-lg"
        />
        <Button onClick={saveCroppedImage}>{t('save')}</Button>
      </div>
    </div>
  );
}
