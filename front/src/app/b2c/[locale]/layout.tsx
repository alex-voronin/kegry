import type { Metadata, Viewport } from 'next';
import { Geist, Geist_Mono, Fondamento } from 'next/font/google';
import { headers } from 'next/headers';
import '../../globals.css';
import { Providers } from '@/components/providers';
import { getMessages, getLocale } from 'next-intl/server';
import { routing } from '@/i18n/routing';
import { ToastContainer } from 'react-toastify';
import { BRAND_NAME } from '@/lib/constants';

const geistSans = Geist({
  variable: '--font-geist-sans',
  subsets: ['latin'],
});

const geistMono = Geist_Mono({
  variable: '--font-geist-mono',
  subsets: ['latin'],
});

const fundamento = Fondamento({
  weight: '400',
  subsets: ['latin'],
  variable: '--font-fundamento',
});

export const metadata: Metadata = {
  title: BRAND_NAME,
  description: 'The best stories start off-screen',
  manifest: '/manifest.json',
  icons: {
    icon: [
      { url: 'icons/favicon.ico', type: 'image/x-icon' },
      { url: '/icons/favicon-16.png', sizes: '16x16', type: 'image/png' },
      { url: '/icons/favicon-32.png', sizes: '32x32', type: 'image/png' },
      { url: '/icons/favicon-48.png', sizes: '48x48', type: 'image/png' },
    ],
    apple: [
      { url: '/icons/favicon-192.png', sizes: '192x192', type: 'image/png' },
    ],
  },
};

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
  maximumScale: 1,
  userScalable: false,
  interactiveWidget: 'resizes-content',
};

export async function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const locale = await getLocale();
  const messages = await getMessages();

  return (
    <html lang={locale}>
      <body
        className={`${geistSans.variable} ${geistMono.variable} ${fundamento.variable} antialiased`}
      >
        <Providers locale={locale} messages={messages}>
          <div className="flex min-h-screen w-full justify-center">
            <main className="bg-surface text-foreground relative flex w-full max-w-[500px] flex-col sm:rounded-[16px]">
              <ToastContainer
                theme="dark"
                position="bottom-center"
                autoClose={5000}
                hideProgressBar={false}
                newestOnTop={true}
                closeOnClick
                rtl={false}
                pauseOnFocusLoss
                pauseOnHover
              />
              {children}
            </main>
          </div>
        </Providers>
      </body>
    </html>
  );
}
