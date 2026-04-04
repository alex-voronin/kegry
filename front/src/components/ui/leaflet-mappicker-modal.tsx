'use client';

import { useEffect, useState } from 'react';
import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  DialogTitle,
  Transition,
  TransitionChild,
} from '@headlessui/react';
import { Button, ButtonVariant } from './button';
import { useTranslations } from 'next-intl';
import dynamic from 'next/dynamic';

const LeafletMapInternal = dynamic(() => import('./leaflet-map-internal'), {
  ssr: false,
  loading: () => (
    <div className="flex h-full w-full items-center justify-center bg-[#1A1A1A]">
      <div className="h-8 w-8 animate-spin rounded-full border-b-2 border-primary"></div>
    </div>
  ),
});

interface LeafletMapPickerModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialLat?: number;
  initialLng?: number;
  onSave: (lat: number, lng: number) => void;
}

const DEFAULT_CENTER = { lat: 44.8125, lng: 20.4612 };

export function LeafletMapPickerModal({
  isOpen,
  onClose,
  initialLat,
  initialLng,
  onSave,
}: LeafletMapPickerModalProps) {
  const t = useTranslations('components.mapPicker');
  const [selectedLocation, setSelectedLocation] = useState<{
    lat: number;
    lng: number;
  } | null>(null);

  // Reset state on open
  useEffect(() => {
    if (isOpen) {
      const lat = initialLat && initialLat !== 0 ? initialLat : DEFAULT_CENTER.lat;
      const lng = initialLng && initialLng !== 0 ? initialLng : DEFAULT_CENTER.lng;
      setSelectedLocation({ lat, lng });
    }
  }, [isOpen, initialLat, initialLng]);

  const handleSave = () => {
    if (selectedLocation) {
      onSave(selectedLocation.lat, selectedLocation.lng);
    }
    onClose();
  };

  return (
    <Transition show={isOpen}>
      <Dialog as="div" className="relative z-[9999]" onClose={onClose}>
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

        <div className="fixed inset-0 z-[10000] flex items-center justify-center p-4">
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
                {isOpen && selectedLocation && (
                  <LeafletMapInternal 
                    center={selectedLocation} 
                    onLocationSelect={(lat, lng) => setSelectedLocation({ lat, lng })}
                  />
                )}
                <div className="absolute bottom-0 right-0 z-1000 bg-black/40 px-2 py-0.5 text-[10px] text-zinc-300 backdrop-blur-sm pointer-events-none">
                  © OpenStreetMap contributors
                </div>
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
