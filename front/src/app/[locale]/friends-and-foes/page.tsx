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
import { ReportDialog } from '@/components/ui/report-dialog';
import { InviteDialog } from '@/components/ui/invite-dialog';
import {
  useBlockedUsersStore,
  BlockedUserDto,
} from '@/store/use-blocked-users-store';
import { useFriendsStore, FriendUserDto } from '@/store/use-friends-store';
import { useMetNpcsStore } from '@/store/use-met-npcs-store';
import { toast } from 'react-toastify';
import { PAGE_ROUTES } from '@/lib/routes';
import { InviteExpiration } from '@/lib/invite-utils';
import styles from './friends-and-foes.module.css';

type Tab = 'friends' | 'imaginary' | 'blocked';

type SelectedUser = {
  id: string;
  firstName: string;
  lastName: string;
  avatarUrl: string | null;
  type: 'friend' | 'blocked';
};

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
  const [selectedUser, setSelectedUser] = useState<SelectedUser | null>(null);
  const [isReportDialogOpen, setIsReportDialogOpen] = useState(false);
  const [userToReport, setUserToReport] = useState<SelectedUser | null>(null);

  const {
    friends,
    isLoading: friendsLoading,
    fetchFriends,
    removeFriend,
    blockFriend,
    inviteFriend,
  } = useFriendsStore();

  const [isInviteDialogOpen, setIsInviteDialogOpen] = useState(false);
  const [userToInvite, setUserToInvite] = useState<FriendUserDto | null>(null);

  const {
    blockedUsers,
    isLoading: blockedLoading,
    fetchBlockedUsers,
    unblockUser,
  } = useBlockedUsersStore();

  const {
    metNpcs,
    isLoading: imaginaryLoading,
    fetchMetNpcs,
  } = useMetNpcsStore();

  useEffect(() => {
    if (activeTab === 'friends') {
      const loadFriends = async () => {
        try {
          await fetchFriends();
        } catch (err: any) {
          toast.error(err.message || t('toast.fetchFailed'));
        }
      };
      loadFriends();
    }

    if (activeTab === 'imaginary') {
      const loadMetNpcs = async () => {
        try {
          await fetchMetNpcs();
        } catch (err: any) {
          toast.error(err.message || t('toast.fetchImaginaryFailed'));
        }
      };
      loadMetNpcs();
    }

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
  }, [activeTab, fetchFriends, fetchMetNpcs, fetchBlockedUsers, t, tBlocked]);

  const handleRemoveFriend = async (user: SelectedUser) => {
    try {
      await removeFriend(user.id);
      toast.success(t('toast.removed', { name: user.firstName }));
    } catch (err: any) {
      toast.error(err.message || t('toast.removeFailed'));
    }
  };

  const handleInviteConfirm = async (when: InviteExpiration) => {
    if (!userToInvite) return;
    try {
      await inviteFriend(userToInvite.id, when);
      toast.success(t('toast.invited', { name: userToInvite.firstName }));
    } catch (err: any) {
      toast.error(err.message || t('toast.inviteFailed'));
    } finally {
      setUserToInvite(null);
    }
  };

  const handleBlockConfirm = async (message: string) => {
    if (!userToReport) return;
    try {
      await blockFriend(userToReport.id, message);
      toast.success(t('toast.blocked', { name: userToReport.firstName }));
    } catch (err: any) {
      toast.error(err.message || 'Failed to block user');
    } finally {
      setUserToReport(null);
    }
  };

  const handleUnblock = async (user: SelectedUser) => {
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

  const getMenuOptions = () => {
    if (!selectedUser) return [];

    if (selectedUser.type === 'friend') {
      return [
        {
          icon: '/assets/feed/invite-icon.webp',
          label: t('menu.invite'),
          onClick: () => {
            setUserToInvite(selectedUser);
            setSelectedUser(null);
            setIsInviteDialogOpen(true);
          },
        },
        {
          icon: '/assets/friends-and-foes/unblock-icon.svg',
          label: t('menu.removeFriend'),
          onClick: () => {
            handleRemoveFriend(selectedUser);
            setSelectedUser(null);
          },
        },
        {
          icon: '/assets/friends-and-foes/user-profile-icon.svg',
          label: t('menu.blockAndReport'),
          onClick: () => {
            setUserToReport(selectedUser);
            setSelectedUser(null);
            setIsReportDialogOpen(true);
          },
        },
      ];
    }

    // Blocked user menu
    return [
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
    ];
  };

  const renderFriendsTab = () => {
    if (friendsLoading && friends.length === 0) {
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
            onClick={() => handleViewProfile(user.id)}
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

            <div
              onClick={(e) => {
                e.stopPropagation();
                setSelectedUser({ ...user, type: 'friend' });
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
  };

  const renderImaginaryTab = () => {
    if (imaginaryLoading && metNpcs.length === 0) {
      return (
        <div className={styles.emptyState}>
          <Spinner />
        </div>
      );
    }

    if (metNpcs.length === 0) {
      return (
        <div className={styles.emptyState}>
          <span>{t('noImaginary')}</span>
        </div>
      );
    }

    return (
      <div className={styles.userList}>
        {metNpcs.map((npc) => (
          <div key={npc.id} className={styles.userItem}>
            <div className={styles.userItemLeft}>
              <div className={styles.profileImageWrapper}>
                <Image
                  src={
                    npc.avatarUrl || '/assets/common/avatar-placeholder.webp'
                  }
                  alt="NPC"
                  width={48}
                  height={48}
                  className="object-cover"
                />
              </div>
              <div className={styles.userBody}>
                <span className={styles.userName}>{npc.name}</span>
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  };

  const renderBlockedTab = () => {
    if (blockedLoading && blockedUsers.length === 0) {
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
            onClick={() => setSelectedUser({ ...user, type: 'blocked' })}
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

  const renderTabContent = () => {
    if (activeTab === 'friends') return renderFriendsTab();

    if (activeTab === 'imaginary') return renderImaginaryTab();

    return renderBlockedTab();
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
        options={getMenuOptions()}
      />

      <ReportDialog
        isOpen={isReportDialogOpen}
        onClose={() => setIsReportDialogOpen(false)}
        onConfirm={handleBlockConfirm}
      />

      <InviteDialog
        isOpen={isInviteDialogOpen}
        friendName={userToInvite?.firstName || ''}
        onClose={() => setIsInviteDialogOpen(false)}
        onConfirm={handleInviteConfirm}
      />
    </div>
  );
}
