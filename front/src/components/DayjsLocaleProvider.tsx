'use client';

import { useEffect, useRef } from 'react';
import dayjs from 'dayjs';
import { useLocale } from 'next-intl';

export function DayjsLocaleProvider() {
  const locale = useLocale();
  const isMounted = useRef(true);

  useEffect(() => {
    isMounted.current = true;

    import(`dayjs/locale/${locale}.js`)
      .then(() => {
        if (isMounted.current) dayjs.locale(locale);
      })
      .catch(() => {
        if (isMounted.current) dayjs.locale('en');
      });

    return () => {
      isMounted.current = false;
    };
  }, [locale]);

  return null;
}
