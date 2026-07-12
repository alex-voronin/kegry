'use client';

import { useRouter } from '@/i18n/routing';
import { Icon } from '@/components/ui/icon';
import { Badge } from '@/components/ui/badge';
import { PAGE_ROUTES } from '@/lib/routes';
import {
  useNotificationCountStore,
  useNotificationCountInit,
} from '@/store/use-notification-count-store';

interface NotificationsButtonProps {
  onClick?: () => void;
}

export function NotificationsButton({ onClick }: NotificationsButtonProps) {
  const router = useRouter();
  useNotificationCountInit();

  const unseenCount = useNotificationCountStore((s) => s.unseenCount);

  const handleClick = onClick ?? (() => router.push(PAGE_ROUTES.NOTIFICATIONS));

  return (
    <Icon
      src="/assets/feed/notifications-icon.svg"
      alt="notifications"
      width={28}
      height={30}
      priority
      onClick={handleClick}
    >
      {unseenCount > 0 && <Badge count={unseenCount} className="absolute -top-1 -right-1" />}
    </Icon>
  );
}
