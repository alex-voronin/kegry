'use client';

import { ReactNode } from 'react';
import { NextIntlClientProvider } from 'next-intl';
import { useBackgroundLocation } from '@/lib/use-background-location';
import { useFcm } from '@/lib/use-fcm';

function BackgroundServices() {
  useBackgroundLocation();
  useFcm();
  return null;
}

export function Providers({
  children,
  locale,
  messages,
}: {
  children: ReactNode;
  locale: string;
  messages: Record<string, any>;
}) {
  return (
    <NextIntlClientProvider messages={messages} locale={locale} timeZone="UTC">
      <BackgroundServices />
      {children}
    </NextIntlClientProvider>
  );
}
