import type { NextConfig } from 'next';
import createNextIntlPlugin from 'next-intl/plugin';
import { version } from './package.json';

const withNextIntl = createNextIntlPlugin('./src/i18n/request.ts');

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

console.log(`api calls will be redirected to: ${API_BASE_URL}`);

const nextConfig: NextConfig = {
  reactStrictMode: true,
  env: {
    NEXT_PUBLIC_APP_VERSION: version,
  },
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: `${API_BASE_URL}/api/:path*`,
      },
    ];
  },
};

export default withNextIntl(nextConfig);
