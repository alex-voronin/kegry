'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { ChevronDown } from 'lucide-react';
import { ModalMenu } from '@/components/ui/modal-menu';
import { City, BELGRADE_COORDS, NOVI_SAD_COORDS, LAST_CITY_KEY } from '@/lib/cities';

interface CitySelectorProps {
  selectedCity: City | null;
  onCityChange: (city: City | null) => void;
  isLocationDenied: boolean;
}

export function CitySelector({
  selectedCity,
  onCityChange,
  isLocationDenied,
}: CitySelectorProps) {
  const tCities = useTranslations('common.cities');
  const [isOpen, setIsOpen] = useState(false);

  const CITIES: Record<string, City> = {
    Belgrade: { ...BELGRADE_COORDS, label: tCities('Belgrade') },
    NoviSad: { ...NOVI_SAD_COORDS, label: tCities('NoviSad') },
  };

  const options = [
    ...(!isLocationDenied
      ? [
          {
            label: tCities('Nearby'),
            onClick: () => {
              onCityChange(null);
              setIsOpen(false);
            },
          },
        ]
      : []),
    {
      label: tCities('Belgrade'),
      onClick: () => {
        onCityChange(CITIES.Belgrade);
        setIsOpen(false);
      },
    },
    {
      label: tCities('NoviSad'),
      onClick: () => {
        onCityChange(CITIES.NoviSad);
        setIsOpen(false);
      },
    },
  ];

  return (
    <>
      <div
        className="flex cursor-pointer items-center gap-1"
        onClick={() => setIsOpen(true)}
      >
        <span className="text-lg font-semibold">
          {selectedCity ? selectedCity.label : tCities('Nearby')}
        </span>
        <ChevronDown size={20} />
      </div>

      <ModalMenu
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        options={options}
      />
    </>
  );
}
