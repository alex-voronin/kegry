'use client';

import { useState, useCallback, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import Cropper from 'react-easy-crop';
import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  DialogTitle,
  Transition,
  TransitionChild,
} from '@headlessui/react';
import { Button, ButtonVariant } from './button';
import { getCroppedImg } from '@/lib/image-utils';

interface ImageCropperModalProps {
  isOpen: boolean;
  onClose: () => void;
  imageSrc: string | null;
  onCropComplete: (croppedBlob: Blob) => void;
  outputSize?: { width: number; height: number };
  cropShape?: 'rect' | 'round';
}

export function ImageCropperModal({
  isOpen,
  onClose,
  imageSrc,
  onCropComplete,
  outputSize,
  cropShape = 'rect',
}: ImageCropperModalProps) {
  const t = useTranslations('components.imageCropper');

  const calculatedAspect = outputSize
    ? outputSize.width / outputSize.height
    : 1;

  const [crop, setCrop] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [croppedAreaPixels, setCroppedAreaPixels] = useState(null);
  const [isShown, setIsShown] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setCrop({ x: 0, y: 0 });
      setZoom(1);
      setCroppedAreaPixels(null);
    }
  }, [isOpen]);

  const handleCropComplete = useCallback(
    (croppedArea: any, croppedAreaPixels: any) => {
      setCroppedAreaPixels(croppedAreaPixels);
    },
    [],
  );

  const handleSave = async () => {
    if (imageSrc && croppedAreaPixels) {
      try {
        const croppedImage = await getCroppedImg(
          imageSrc,
          croppedAreaPixels,
          undefined,
          outputSize,
        );
        if (croppedImage) {
          onCropComplete(croppedImage);
        }
      } catch (e) {
        console.error(e);
      }
    }
  };

  return (
    <Transition
      show={isOpen}
      afterEnter={() => setIsShown(true)}
      afterLeave={() => setIsShown(false)}
    >
      <Dialog as="div" className="relative z-9999" onClose={onClose}>
        <TransitionChild
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <DialogBackdrop className="fixed inset-0 bg-black/80 backdrop-blur-sm" />
        </TransitionChild>

        <div className="fixed inset-0 z-10000 flex items-center justify-center p-4">
          <TransitionChild
            enter="ease-out duration-300"
            enterFrom="opacity-0 scale-95"
            enterTo="opacity-100 scale-100"
            leave="ease-in duration-200"
            leaveFrom="opacity-100 scale-100"
            leaveTo="opacity-0 scale-95"
          >
            <DialogPanel className="flex h-[85vh] w-full max-w-4xl flex-col overflow-hidden rounded-2xl border border-white/10 bg-[#1A1A1A] shadow-2xl">
              <div className="flex items-center justify-between border-b border-white/10 p-4">
                <DialogTitle className="text-lg font-semibold text-white">
                  {t('title')}
                </DialogTitle>
                <div className="flex gap-2">
                  <Button variant={ButtonVariant.Secondary} onClick={onClose}>
                    {t('cancel')}
                  </Button>
                  <Button variant={ButtonVariant.Default} onClick={handleSave}>
                    {t('save')}
                  </Button>
                </div>
              </div>

              <div className="relative flex-1 bg-zinc-900">
                {imageSrc && isShown && (
                  <Cropper
                    image={imageSrc}
                    crop={crop}
                    zoom={zoom}
                    showGrid={false}
                    cropShape={cropShape}
                    aspect={calculatedAspect}
                    onCropChange={setCrop}
                    onCropComplete={handleCropComplete}
                    onZoomChange={setZoom}
                  />
                )}
              </div>

              <div className="flex items-center justify-center border-t border-white/10 p-4">
                <div className="flex w-full max-w-sm items-center gap-4">
                  <span className="text-xs text-white/50">1x</span>
                  <input
                    type="range"
                    min={1}
                    max={3}
                    step={0.01}
                    value={zoom}
                    onChange={(e) => setZoom(Number(e.target.value))}
                    className="accent-primary bg-secondary h-2 flex-1 rounded-lg"
                  />
                  <span className="text-xs text-white/50">3x</span>
                </div>
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
