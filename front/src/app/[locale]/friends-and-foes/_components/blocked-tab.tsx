import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { useBlockedUsersStore } from '@/store/use-blocked-users-store';
import styles from '../friends-and-foes.module.css';
import { SelectedUser } from '../types';

interface BlockedTabProps {
  onSelectUser: (user: SelectedUser) => void;
}

export function BlockedTab({ onSelectUser }: BlockedTabProps) {
  const tBlocked = useTranslations('screens.blockedUsers');
  const { blockedUsers, isLoading } = useBlockedUsersStore();

  if (isLoading && blockedUsers.length === 0) {
    return (
      <div className={styles.emptyState}>
        <Spinner />
      </div>
    );
  }

  if (blockedUsers.length === 0) {
    return (
      <div className={styles.emptyState}>
        <span>{tBlocked('noBlockedUsers')}</span>
      </div>
    );
  }

  return (
    <div className={styles.userList}>
      {blockedUsers.map((user) => (
        <div
          key={user.id}
          className={styles.userItem}
          onClick={() => onSelectUser({ ...user, type: 'blocked' })}
        >
          <div className={styles.userItemLeft}>
            <div className={styles.profileImageWrapper}>
              <Image
                src={user.avatarUrl || '/assets/common/avatar-placeholder.webp'}
                alt="Profile"
                width={48}
                height={48}
                className="object-cover"
              />
            </div>
            <div className={styles.userBody}>
              <span className={styles.userName}>
                {user.firstName} {user.lastName}
              </span>
            </div>
          </div>

          <Icon
            src="/assets/common/more-icon.svg"
            alt="Open menu"
            width={24}
            height={24}
            priority
          />
        </div>
      ))}
    </div>
  );
}
