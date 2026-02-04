import type { Metadata } from 'next';
import { Geist, Geist_Mono } from 'next/font/google';
import { headers } from 'next/headers';
import '../globals.css';
import { Providers } from '@/components/providers';
import { getMessages, getLocale } from 'next-intl/server';
import { routing } from '@/i18n/routing';
import { ToastContainer } from 'react-toastify';

const geistSans = Geist({
  variable: '--font-geist-sans',
  subsets: ['latin'],
});

const geistMono = Geist_Mono({
  variable: '--font-geist-mono',
  subsets: ['latin'],
});

export const metadata: Metadata = {
  title: 'Hoppyfy.me',
  description: 'The best stories start off-screen',
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
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
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
