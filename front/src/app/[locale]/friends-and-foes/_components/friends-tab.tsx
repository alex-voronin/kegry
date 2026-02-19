import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { useFriendsStore } from '@/store/use-friends-store';
import styles from '../friends-and-foes.module.css';
import { SelectedUser } from '../types';

interface FriendsTabProps {
  onSelectUser: (user: SelectedUser) => void;
  onViewProfile: (userId: string) => void;
}

export function FriendsTab({ onSelectUser, onViewProfile }: FriendsTabProps) {
  const t = useTranslations('screens.friendsAndFoes');
  const { friends, isLoading } = useFriendsStore();

  if (isLoading && friends.length === 0) {
    return (
      <div className={styles.emptyState}>
        <Spinner />
      </div>
    );
  }

  if (friends.length === 0) {
    return (
      <div className={styles.emptyState}>
        <span>{t('noFriends')}</span>
      </div>
    );
  }

  return (
    <div className={styles.userList}>
      {friends.map((user) => (
        <div
          key={user.id}
          className={styles.userItem}
          onClick={() => onSelectUser({ ...user, type: 'friend' })}
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

          <div
            onClick={(e) => {
              e.stopPropagation();
              onSelectUser({ ...user, type: 'friend' });
            }}
          >
            <Icon
              src="/assets/common/more-icon.svg"
              alt="Open menu"
              width={24}
              height={24}
              priority
            />
          </div>
        </div>
      ))}
    </div>
  );
}
