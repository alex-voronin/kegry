'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { SegmentedControl } from '@/components/ui/segmented-control';
import { useDiscountsStore } from './use-discounts-store';
import { usePromotionsStore } from './use-promotions-store';
import { PAGE_ROUTES } from '@/lib/routes';
import { DiscountsTab } from './_components/discounts-tab';
import { PromotionsTab } from './_components/promotions-tab';
import { City, BELGRADE_COORDS, LAST_CITY_KEY } from '@/lib/cities';
import { CitySelector } from '@/components/common/city-selector';
import { checkPermissionStatus } from '@/lib/permissions';
import { useCitySelection } from '@/hooks/use-city-selection';
import styles from './page.module.css';

type Tab = 'discounts' | 'promotions';

export default function DiscountsAndPromotionsScreen() {
  const router = useRouter();
  const t = useTranslations('screens.discountsAndPromotions');
  const tCities = useTranslations('common.cities');

  const TAB_OPTIONS = [
    { value: 'discounts', label: t('tabs.discounts'), width: '50%' },
    { value: 'promotions', label: t('tabs.promotions'), width: '50%' },
  ];

  const [activeTab, setActiveTab] = useState<Tab>('discounts');
  const { selectedCity, hasHydrated, handleCityChange } = useCitySelection();
  const [isLocationDenied, setIsLocationDenied] = useState(false);

  useEffect(() => {
    const check = async () => {
      const { locationAllowed } = await checkPermissionStatus();
      setIsLocationDenied(!locationAllowed);
    };
    check();

    window.addEventListener('focus', check);
    return () => {
      window.removeEventListener('focus', check);
      useDiscountsStore.getState().reset();
      usePromotionsStore.getState().reset();
    };
  }, []);

  // Fallback to Belgrade if Nearby is selected but location is denied
  useEffect(() => {
    if (hasHydrated && isLocationDenied && !selectedCity) {
      const belgrade = {
        ...BELGRADE_COORDS,
        label: tCities('Belgrade'),
      };
      handleCityChange(belgrade);
    }
  }, [hasHydrated, isLocationDenied, selectedCity, handleCityChange, tCities]);

  const renderTabContent = () => {
    if (!hasHydrated) return null;

    if (activeTab === 'discounts') {
      return (
        <DiscountsTab
          selectedCity={selectedCity}
          isLocationDenied={isLocationDenied}
        />
      );
    }
    return (
      <PromotionsTab
        selectedCity={selectedCity}
        isLocationDenied={isLocationDenied}
      />
    );
  };

  return (
    <div className="flex flex-1 flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => router.replace(PAGE_ROUTES.PROFILE)}
            priority
          />
        }
        title={
          <CitySelector
            selectedCity={selectedCity}
            onCityChange={handleCityChange}
            isLocationDenied={isLocationDenied}
          />
        }
      />

      <div className={styles.tabBar}>
        <SegmentedControl
          options={TAB_OPTIONS}
          value={activeTab}
          onChange={(v) => setActiveTab(v as Tab)}
        />
      </div>

      <div className={styles.content}>{renderTabContent()}</div>
    </div>
  );
}
