import { ComponentType, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { PAGE_ROUTES } from '../routes';
import { useAuthStore } from './use-auth-store';

export function withAuth<P extends object>(Component: ComponentType<P>) {
  return function ProtectedPage(props: P) {
    const { userId, hasHydrated } = useAuthStore();
    const router = useRouter();

    useEffect(() => {
      if (hasHydrated && !userId) {
        router.push(PAGE_ROUTES.LOGIN);
      }
    }, [userId, router, hasHydrated]);

    if (!userId) return null;

    return <Component {...props} />;
  };
}
