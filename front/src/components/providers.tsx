"use client";

import { ReactNode } from "react";
import { NextIntlClientProvider } from 'next-intl';

export function Providers({
    children,
    locale,
    messages
}: {
    children: ReactNode;
    locale: string;
    messages: Record<string, any>;
}) {
    return (
        <NextIntlClientProvider messages={messages} locale={locale} timeZone="UTC">
            {children}
        </NextIntlClientProvider>
    );
}
