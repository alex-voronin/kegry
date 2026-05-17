import type { NextConfig } from 'next';
import createNextIntlPlugin from 'next-intl/plugin';
import { version } from './package.json';

const withNextIntl = createNextIntlPlugin('./src/i18n/request.ts');

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

console.log(`api calls will be redirected to: ${API_BASE_URL}`);

const nextConfig: NextConfig = {
  output: 'standalone',
  reactStrictMode: true,
  env: {
    NEXT_PUBLIC_APP_VERSION: version,
  },
  allowedDevOrigins: ['localhost:3000', '10.140.79.179', '10.240.27.179'],
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'images.kegry.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'images.kegry.rs',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'pub-c880f33c01db4fa4a8013b557b07085f.r2.dev',
        port: '',
        pathname: '/**',
      },
    ],
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
