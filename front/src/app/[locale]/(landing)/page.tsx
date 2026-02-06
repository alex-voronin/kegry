'use client';

import { useMediaQuery } from '@/hooks/use-media-query';
import DesktopLayout from './_components/desktop-layout';
import MobileLayout from './_components/mobile-layout';

export default function LandingPage() {
  const isMobile = useMediaQuery('(max-width: 900px)');

  // Don't render anything until we know the viewport size to prevent flash
  if (isMobile === undefined) {
    return null;
  }

  return isMobile ? <MobileLayout /> : <DesktopLayout />;
}
