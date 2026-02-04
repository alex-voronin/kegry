'use client';

import { useMediaQuery } from '@/hooks/use-media-query';
import DesktopLayout from './_components/desktop-layout';
import MobileLayout from './_components/mobile-layout';

export default function LandingPage() {
  const isMobile = useMediaQuery('(max-width: 768px)');

  return isMobile ? <MobileLayout /> : <DesktopLayout />;
}
