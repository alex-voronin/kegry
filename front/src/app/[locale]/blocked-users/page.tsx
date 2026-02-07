'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Spinner } from '@/components/ui/spinner';
import {
  useBlockedUsersStore,
  BlockedUserDto,
} from '@/store/use-blocked-users-store';
import { toast } from 'react-toastify';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from './blocked-users.module.css';

export default function BlockedUsersPage() {
  const router = useRouter();
  const t = useTranslations('screens.blockedUsers');

  const [selectedUser, setSelectedUser] = useState<BlockedUserDto | null>(null);

  const { blockedUsers, isLoading, fetchBlockedUsers, unblockUser } =
    useBlockedUsersStore();

  useEffect(() => {
    const loadBlockedUsers = async () => {
      try {
        await fetchBlockedUsers();
      } catch (err: any) {
        toast.error(err.message || t('toast.fetchFailed'));
      }
    };
    loadBlockedUsers();
  }, [fetchBlockedUsers]);

  const handleUnblock = async (user: BlockedUserDto) => {
    try {
      await unblockUser(user.id);
      toast.success(t('toast.unblocked', { name: user.firstName }));
    } catch (err: any) {
      toast.error(err.message || t('toast.unblockFailed'));
    }
  };

  const handleViewProfile = (userId: string) => {
    router.push(`${PAGE_ROUTES.PROFILE}/${userId}`);
  };

  const menuOptions = selectedUser
    ? [
        {
          icon: '/assets/blocked-users/user-profile-icon.svg',
          label: t('menu.viewProfile'),
          onClick: () => {
            handleViewProfile(selectedUser.id);
            setSelectedUser(null);
          },
        },
        {
          icon: '/assets/blocked-users/unblock-icon.svg',
          label: t('menu.unblock'),
          onClick: () => {
            handleUnblock(selectedUser);
            setSelectedUser(null);
          },
        },
      ]
    : [];

  return (
    <div className="flex flex-1 flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={20}
            height={26}
            onClick={() => router.back()}
            priority
          />
        }
        title={t('title')}
      />

      <div className={styles.content}>
        {isLoading && blockedUsers.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : blockedUsers.length === 0 ? (
          <div className={styles.emptyState}>
            <span>{t('noBlockedUsers')}</span>
          </div>
        ) : (
          <div className={styles.blockedList}>
            {blockedUsers.map((user) => (
              <div
                key={user.id}
                className={styles.blockedItem}
                onClick={() => setSelectedUser(user)}
              >
                <div className={styles.blockedItemLeft}>
                  <div className={styles.profileImageWrapper}>
                    <Image
                      src={
                        user.avatarUrl ||
                        '/assets/common/avatar-placeholder.webp'
                      }
                      alt="Profile"
                      width={48}
                      height={48}
                      className="object-cover"
                    />
                  </div>
                  <div className={styles.blockedBody}>
                    <span className={styles.blockedName}>
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
        )}
      </div>

      <ModalMenu
        isOpen={!!selectedUser}
        onClose={() => setSelectedUser(null)}
        options={menuOptions}
      />
    </div>
  );
}
