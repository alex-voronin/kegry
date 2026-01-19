import { ComponentType, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from './useAuthStore';

export function withAuth<P extends object>(Component: ComponentType<P>) {
  return function ProtectedPage(props: P) {
    const { userId, hasHydrated } = useAuthStore();
    const router = useRouter();

    useEffect(() => {
      if (hasHydrated && !userId) {
        router.push('/auth/login');
      }
    }, [userId, router, hasHydrated]);

    if (!userId) return null;

    return <Component {...props} />;
  };
}
