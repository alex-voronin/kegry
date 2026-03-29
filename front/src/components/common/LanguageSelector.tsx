'use client';

import React from 'react';
import { Menu, MenuButton, MenuItem, MenuItems } from '@headlessui/react';
import { ChevronDown } from 'lucide-react';
import { useLocale } from 'next-intl';
import { usePathname, useRouter } from '@/i18n/routing';
import styles from './LanguageSelector.module.css';

const locales = [
  {
    code: 'en',
    label: 'English',
    shortLabel: 'EN',
    flag: '🇬🇧',
  },
  {
    code: 'sr',
    label: 'Srpski',
    shortLabel: 'SR',
    flag: '🇷🇸',
  },
] as const;

export function LanguageSelector() {
  const locale = useLocale();
  const pathname = usePathname();
  const router = useRouter();

  const currentLocale = locales.find((l) => l.code === locale) || locales[0];

  const handleLocaleChange = (newLocale: 'en' | 'sr') => {
    router.replace(pathname, { locale: newLocale });
  };

  return (
    <div className={styles.container}>
      <Menu>
        {({ open }) => (
          <>
            <MenuButton className={styles.trigger} data-open={open}>
              <span className={styles.localeText}>
                {currentLocale.shortLabel}
              </span>
              <ChevronDown className={styles.chevron} />
            </MenuButton>
            <MenuItems anchor="bottom start" className={styles.options}>
              {locales.map((item) => (
                <MenuItem key={item.code}>
                  <button
                    className={`${styles.option} ${
                      locale === item.code ? styles.optionActive : ''
                    }`}
                    onClick={() => handleLocaleChange(item.code as 'en' | 'sr')}
                  >
                    <span className={styles.flag}>{item.flag}</span>
                    <div className={styles.labelWrapper}>
                      <span className={styles.fullName}>{item.label}</span>
                      <span className={styles.shortName}>
                        ({item.shortLabel})
                      </span>
                    </div>
                  </button>
                </MenuItem>
              ))}
            </MenuItems>
          </>
        )}
      </Menu>
    </div>
  );
}
