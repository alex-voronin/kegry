import { useState, useEffect } from 'react';
import { City, LAST_CITY_KEY } from '@/lib/cities';

export function useCitySelection() {
  const [selectedCity, setSelectedCity] = useState<City | null>(null);
  const [hasHydrated, setHasHydrated] = useState(false);

  useEffect(() => {
    const saved = localStorage.getItem(LAST_CITY_KEY);
    if (saved) {
      try {
        setSelectedCity(JSON.parse(saved));
      } catch {
        localStorage.removeItem(LAST_CITY_KEY);
      }
    }
    setHasHydrated(true);
  }, []);

  const handleCityChange = (city: City | null) => {
    if (city) {
      localStorage.setItem(LAST_CITY_KEY, JSON.stringify(city));
    } else {
      localStorage.removeItem(LAST_CITY_KEY);
    }
    setSelectedCity(city);
  };

  return { selectedCity, hasHydrated, handleCityChange };
}
