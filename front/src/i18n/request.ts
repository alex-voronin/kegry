import { headers } from 'next/headers';
import { getRequestConfig } from 'next-intl/server';
import { routing } from './routing';
import { B2B_SUBDOMAIN } from '@/lib/constants';

export default getRequestConfig(async ({ requestLocale }) => {
  let locale = await requestLocale;

  // Ensure that the incoming locale is valid
  if (!locale || !routing.locales.includes(locale as any)) {
    locale = routing.defaultLocale;
  }

  // Determine platform from host header
  const host = (await headers()).get('host') || '';
  const platform = host.startsWith(B2B_SUBDOMAIN) ? 'b2b' : 'b2c';

  return {
    locale,
    messages: (await import(`../../messages/${platform}/${locale}.json`))
      .default,
  };
});
