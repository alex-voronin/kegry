'use client';

import { useState, useCallback } from 'react';
import Cropper from 'react-easy-crop';
import { Button } from './button';

function getCroppedImg(
  imageSrc: string,
  crop: { x: number; y: number; width: number; height: number },
) {
  return new Promise<string>((resolve, reject) => {
    const image = new Image();
    image.src = imageSrc;
    image.onload = () => {
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      if (!ctx) return reject('No ctx');

      canvas.width = crop.width;
      canvas.height = crop.height;

      ctx.drawImage(
        image,
        crop.x,
        crop.y,
        crop.width,
        crop.height,
        0,
        0,
        crop.width,
        crop.height,
      );

      resolve(canvas.toDataURL('image/png'));
    };
    image.onerror = reject;
  });
}

export default function ProfileImageCropper({
  imageSrc,
  onCrop,
}: {
  imageSrc: string;
  onCrop: (base64ProfileImage: string) => void;
}) {
  const [crop, setCrop] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<any>(null);

  const onCropComplete = useCallback((_: any, croppedAreaPixels: any) => {
    setCroppedAreaPixels(croppedAreaPixels);
  }, []);

  const saveCroppedImage = useCallback(async () => {
    if (!croppedAreaPixels) return;
    const cropped = await getCroppedImg(imageSrc, croppedAreaPixels);
    onCrop(cropped);
  }, [croppedAreaPixels, imageSrc]);

  return (
    <div className="bg-surface fixed z-50 h-screen w-full">
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
        <Button onClick={saveCroppedImage}>Сохранить</Button>
      </div>
    </div>
  );
}
