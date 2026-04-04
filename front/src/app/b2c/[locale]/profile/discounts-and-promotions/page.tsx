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
import styles from './page.module.css';

type Tab = 'discounts' | 'promotions';

export default function DiscountsAndPromotionsScreen() {
  const router = useRouter();
  const t = useTranslations('screens.discountsAndPromotions');

  const TAB_OPTIONS = [
    { value: 'discounts', label: t('tabs.discounts'), width: '50%' },
    { value: 'promotions', label: t('tabs.promotions'), width: '50%' },
  ];

  const [activeTab, setActiveTab] = useState<Tab>('discounts');

  useEffect(() => {
    return () => {
      useDiscountsStore.getState().reset();
      usePromotionsStore.getState().reset();
    };
  }, []);

  const renderTabContent = () => {
    if (activeTab === 'discounts') {
      return <DiscountsTab />;
    }
    return <PromotionsTab />;
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
        title={t('title')}
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
