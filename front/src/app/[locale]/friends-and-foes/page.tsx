'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { useRouter } from '@/i18n/routing';
import { useSearchParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { SegmentedControl } from '@/components/ui/segmented-control';
import { ReportDialog } from '@/components/ui/report-dialog';
import { InviteDialog } from '@/components/ui/invite-dialog';
import { useBlockedUsersStore } from '@/store/use-blocked-users-store';
import { useFriendsStore, FriendUserDto } from '@/store/use-friends-store';
import { useMetNpcsStore } from '@/store/use-met-npcs-store';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import { toast } from 'react-toastify';
import { PAGE_ROUTES } from '@/lib/routes';
import { InviteExpiration } from '@/lib/invite-utils';
import styles from './friends-and-foes.module.css';
import { SelectedUser } from './types';
import { FriendsTab } from './_components/friends-tab';
import { ImaginaryTab } from './_components/imaginary-tab';
import { BlockedTab } from './_components/blocked-tab';
import { ShareFallbackDialog } from './_components/share-fallback-dialog';

type Tab = 'friends' | 'imaginary' | 'blocked';

export default function FriendsAndFoesPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const t = useTranslations('screens.friendsAndFoes');
  const tCommon = useTranslations('common');
  const tBlocked = useTranslations('screens.blockedUsers');

  const TAB_OPTIONS = [
    { value: 'friends', label: t('tabs.friends'), width: '33%' },
    { value: 'imaginary', label: t('tabs.imaginary'), width: '34%' },
    { value: 'blocked', label: t('tabs.blocked'), width: '33%' },
  ];
  const [activeTab, setActiveTab] = useState<Tab>(() => {
    const tabParam = searchParams.get('tab');
    if (tabParam === 'imaginary' || tabParam === 'blocked') return tabParam;
    return 'friends';
  });
  const [selectedUser, setSelectedUser] = useState<SelectedUser | null>(null);
  const [isReportDialogOpen, setIsReportDialogOpen] = useState(false);
  const [userToReport, setUserToReport] = useState<SelectedUser | null>(null);
  const [isAddMenuOpen, setIsAddMenuOpen] = useState(false);
  const [isShareFallbackOpen, setIsShareFallbackOpen] = useState(false);
  const [shareUrl, setShareUrl] = useState('');

  const { fetchFriends, removeFriend, blockFriend, inviteFriend } =
    useFriendsStore();
  const { userId } = useAuthStore();

  const [isInviteDialogOpen, setIsInviteDialogOpen] = useState(false);
  const [userToInvite, setUserToInvite] = useState<FriendUserDto | null>(null);
  const [selectedNpcCover, setSelectedNpcCover] = useState<{
    name: string;
    coverUrl: string;
  } | null>(null);

  const { fetchBlockedUsers, unblockUser } = useBlockedUsersStore();

  const { fetchMetNpcs } = useMetNpcsStore();

  useEffect(() => {
    if (searchParams.get('share') === 'true') {
      setIsAddMenuOpen(true);
    }
    const tabParam = searchParams.get('tab');
    if (tabParam === 'imaginary' || tabParam === 'blocked') {
      setActiveTab(tabParam);
    }
    // Clean up query params from the URL
    if (searchParams.get('share') || searchParams.get('tab')) {
      router.replace(PAGE_ROUTES.FRIENDS_AND_FOES);
    }
  }, [searchParams, router]);
  useEffect(() => {
    return () => {
      // Clear stores when leaving the page so they refresh when reopened,
      // but tabs themselves control the loading state and timing.
      useFriendsStore.getState().reset();
      useBlockedUsersStore.getState().reset();
      useMetNpcsStore.getState().reset();
    };
  }, []);

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

  const handleShareLink = async () => {
    const shareData = {
      title: 'Wibblycorn',
      text: 'Join me on Wibblycorn!',
      url:
        userId != null
          ? `${window.location.origin}?referrer=${userId}`
          : window.location.origin,
    };

    if (navigator.share) {
      try {
        await navigator.share(shareData);
      } catch (err) {
        console.error('Error sharing:', err);
      }
    } else {
      try {
        await navigator.clipboard.writeText(shareData.url);
        toast.success(tCommon('toast.linkCopied'));
      } catch (err) {
        setShareUrl(shareData.url);
        setIsShareFallbackOpen(true);
      }
    }
    setIsAddMenuOpen(false);
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
          icon: '/assets/friends-and-foes/user-profile-icon.svg',
          label: t('menu.viewProfile'),
          onClick: () => {
            handleViewProfile(selectedUser.id);
            setSelectedUser(null);
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

    if (selectedUser.type === 'imaginary') {
      return [
        {
          icon: '/assets/friends-and-foes/user-profile-icon.svg',
          label: t('imaginaryMenu.show'),
          onClick: () => {
            if (selectedUser.coverUrl) {
              setSelectedNpcCover({
                name: selectedUser.firstName,
                coverUrl: selectedUser.coverUrl,
              });
            }
            setSelectedUser(null);
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

  const renderTabContent = () => {
    if (activeTab === 'friends') {
      return (
        <FriendsTab
          onSelectUser={setSelectedUser}
          onViewProfile={handleViewProfile}
        />
      );
    }

    if (activeTab === 'imaginary') {
      return (
        <ImaginaryTab
          onSelectUser={setSelectedUser}
          onSelectCover={setSelectedNpcCover}
        />
      );
    }

    return <BlockedTab onSelectUser={setSelectedUser} />;
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
        right={
          activeTab === 'friends' && (
            <Icon
              src="/assets/friends-and-foes/plus-icon.svg"
              alt="add friend"
              width={24}
              height={24}
              onClick={() => setIsAddMenuOpen(true)}
              priority
            />
          )
        }
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

      <ModalMenu
        isOpen={isAddMenuOpen}
        onClose={() => setIsAddMenuOpen(false)}
        options={[
          {
            icon: '/assets/friends-and-foes/share-icon.svg',
            label: t('menu.shareLink'),
            onClick: handleShareLink,
          },
        ]}
      />

      <ShareFallbackDialog
        isOpen={isShareFallbackOpen}
        onClose={() => setIsShareFallbackOpen(false)}
        url={shareUrl}
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

      {/* NPC Cover Image Viewer */}
      {selectedNpcCover && (
        <div
          className={styles.coverOverlay}
          onClick={() => setSelectedNpcCover(null)}
        >
          <div className={styles.coverContainer}>
            <Image
              src={selectedNpcCover.coverUrl}
              alt={selectedNpcCover.name}
              fill
              className="object-cover"
            />
          </div>
        </div>
      )}
    </div>
  );
}
