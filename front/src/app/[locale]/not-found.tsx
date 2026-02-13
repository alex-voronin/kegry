'use client';

import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { useRouter } from '@/i18n/routing';
import { Button } from '@/components/ui/button';
import { PAGE_ROUTES } from '@/lib/routes';
import { useAuthStore } from '@/lib/auth/use-auth-store';

export default function NotFound() {
  const t = useTranslations('screens.notFound');
  const router = useRouter();
  const { accessToken, userId } = useAuthStore();

  const handleGoHome = () => {
    if (accessToken && userId) {
      router.push(PAGE_ROUTES.FEED);
    } else {
      router.push(PAGE_ROUTES.HOME);
    }
  };

  return (
    <div className="bg-background flex flex-1 flex-col items-center justify-center p-4 text-center">
      <div className="mb-8 flex justify-center">
        <Image
          src="/assets/not-found/not-found.webp"
          alt="Page not found"
          width={300}
          height={300}
          priority
        />
      </div>

      <p className="text-primary mb-8 text-base">{t('description')}</p>
      <Button onClick={handleGoHome} className="min-w-[200px]">
        {t('home')}
      </Button>
    </div>
  );
}
