'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { Spinner } from '@/components/ui/spinner';
import { SegmentedControl } from '@/components/ui/segmented-control';
import {
  useBlockedUsersStore,
  BlockedUserDto,
} from '@/store/use-blocked-users-store';
import { toast } from 'react-toastify';
import { PAGE_ROUTES } from '@/lib/routes';
import styles from './friends-and-foes.module.css';

type Tab = 'friends' | 'imaginary' | 'blocked';

export default function FriendsAndFoesPage() {
  const router = useRouter();
  const t = useTranslations('screens.friendsAndFoes');
  const tBlocked = useTranslations('screens.blockedUsers');

  const TAB_OPTIONS = [
    { value: 'friends', label: t('tabs.friends'), width: '33%' },
    { value: 'imaginary', label: t('tabs.imaginary'), width: '34%' },
    { value: 'blocked', label: t('tabs.blocked'), width: '33%' },
  ];
  const [activeTab, setActiveTab] = useState<Tab>('friends');
  const [selectedUser, setSelectedUser] = useState<BlockedUserDto | null>(null);

  const { blockedUsers, isLoading, fetchBlockedUsers, unblockUser } =
    useBlockedUsersStore();

  useEffect(() => {
    if (activeTab === 'blocked') {
      const loadBlockedUsers = async () => {
        try {
          await fetchBlockedUsers();
        } catch (err: any) {
          toast.error(err.message || tBlocked('toast.fetchFailed'));
        }
      };
      loadBlockedUsers();
    }
  }, [activeTab, fetchBlockedUsers, tBlocked]);

  const handleUnblock = async (user: BlockedUserDto) => {
    try {
      await unblockUser(user.id);
      toast.success(tBlocked('toast.unblocked', { name: user.firstName }));
    } catch (err: any) {
      toast.error(err.message || tBlocked('toast.unblockFailed'));
    }
  };

  const handleViewProfile = (userId: string) => {
    router.push(`${PAGE_ROUTES.PROFILE}/${userId}`);
  };

  const menuOptions = selectedUser
    ? [
        {
          icon: '/assets/friends-and-foes/user-profile-icon.svg',
          label: tBlocked('menu.viewProfile'),
          onClick: () => {
            handleViewProfile(selectedUser.id);
            setSelectedUser(null);
          },
        },
        {
          icon: '/assets/friends-and-foes/unblock-icon.svg',
          label: tBlocked('menu.unblock'),
          onClick: () => {
            handleUnblock(selectedUser);
            setSelectedUser(null);
          },
        },
      ]
    : [];

  const renderTabContent = () => {
    if (activeTab === 'friends') {
      return (
        <div className={styles.emptyState}>
          <span>{t('comingSoon')}</span>
        </div>
      );
    }

    if (activeTab === 'imaginary') {
      return (
        <div className={styles.emptyState}>
          <span>{t('comingSoon')}</span>
        </div>
      );
    }

    // Blocked tab
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
            onClick={() => setSelectedUser(user)}
          >
            <div className={styles.userItemLeft}>
              <div className={styles.profileImageWrapper}>
                <Image
                  src={
                    user.avatarUrl || '/assets/common/avatar-placeholder.webp'
                  }
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
  };

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

      <div className={styles.tabBar}>
        <SegmentedControl
          options={TAB_OPTIONS}
          value={activeTab}
          onChange={(v) => setActiveTab(v as Tab)}
        />
      </div>

      <div className={styles.content}>{renderTabContent()}</div>

      <ModalMenu
        isOpen={!!selectedUser}
        onClose={() => setSelectedUser(null)}
        options={menuOptions}
      />
    </div>
  );
}
