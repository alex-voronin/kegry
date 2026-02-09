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
  useNotificationsStore,
  NotificationDto,
  isNotificationExpired,
} from '@/store/use-notifications-store';
import { toast } from 'react-toastify';
import styles from './notifications.module.css';

export default function NotificationsScreen() {
  const router = useRouter();
  const t = useTranslations('screens.notifications');
  const [selectedNotification, setSelectedNotification] =
    useState<NotificationDto | null>(null);

  const {
    notifications,
    isLoading,
    fetchNotifications,
    acceptInvite,
    ignoreInvite,
    blockUser,
  } = useNotificationsStore();

  useEffect(() => {
    const loadNotifications = async () => {
      try {
        await fetchNotifications();
      } catch (err: any) {
        toast.error(err.message || t('toast.loadFailed'));
      }
    };
    loadNotifications();
  }, [fetchNotifications, t]);

  const handleAccept = async (notif: NotificationDto) => {
    if (!notif.fromUserId) return;
    if (isNotificationExpired(notif.createdAt)) {
      toast.error(t('toast.expired'));
      return;
    }
    try {
      await acceptInvite(notif.fromUserId, notif.id);
      toast.success(t('toast.accepted'));
    } catch (err: any) {
      toast.error(err.message || t('toast.acceptFailed'));
    }
  };

  const handleIgnore = async (notif: NotificationDto) => {
    if (!notif.fromUserId) return;
    try {
      await ignoreInvite(notif.fromUserId, notif.id);
    } catch (err: any) {
      toast.error(err.message || t('toast.ignoreFailed'));
    }
  };

  const handleBlock = async (notif: NotificationDto) => {
    if (!notif.fromUserId) return;
    try {
      await blockUser(notif.fromUserId, notif.id);
      toast.success(t('toast.blocked'));
    } catch (err: any) {
      toast.error(err.message || t('toast.blockFailed'));
    }
  };

  const getMenuOptions = (notif: NotificationDto) => {
    const options = [];

    if (!isNotificationExpired(notif.createdAt) && notif.fromUserId) {
      options.push({
        icon: '/assets/notifications/accept-icon.webp',
        label: t('menu.accept'),
        onClick: () => {
          handleAccept(notif);
          setSelectedNotification(null);
        },
      });
    }

    if (notif.fromUserId) {
      options.push({
        icon: '/assets/notifications/ignore-icon.svg',
        label: t('menu.ignore'),
        onClick: () => {
          handleIgnore(notif);
          setSelectedNotification(null);
        },
      });
      options.push({
        icon: '/assets/notifications/block-icon.svg',
        label: t('menu.block'),
        onClick: () => {
          handleBlock(notif);
          setSelectedNotification(null);
        },
        isDestructive: true,
      });
    }

    return options;
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

      <div className={styles.content}>
        {isLoading && notifications.length === 0 ? (
          <div className={styles.emptyState}>
            <Spinner />
          </div>
        ) : notifications.length === 0 ? (
          <div className={styles.emptyState}>
            <span>{t('noNotifications')}</span>
          </div>
        ) : (
          <div className={styles.notificationList}>
            {notifications.map((notif) => {
              const expired = isNotificationExpired(notif.createdAt);
              return (
                <div
                  key={notif.id}
                  className={`${styles.notificationItem} ${expired ? styles.expired : ''}`}
                >
                  <div className={styles.notificationItemLeft}>
                    <div className={styles.profileImageWrapper}>
                      <Image
                        src={
                          notif.avatarImageUrl ||
                          '/assets/common/avatar-placeholder.webp'
                        }
                        alt="Profile"
                        width={48}
                        height={48}
                        className="object-cover"
                      />
                    </div>
                    <div className={styles.notificationBody}>
                      <span className={styles.notificationTitle}>
                        {notif.primaryText}
                      </span>
                      <span className={styles.notificationTime}>
                        {notif.secondaryText}
                      </span>
                    </div>
                  </div>

                  <div className={styles.notificationItemRight}>
                    {!expired && notif.fromUserId && (
                      <Icon
                        src="/assets/notifications/accept-icon.webp"
                        alt="accept"
                        width={24}
                        height={24}
                        className="object-fill"
                        priority
                        onClick={() => handleAccept(notif)}
                      />
                    )}
                    <Icon
                      src="/assets/common/more-icon.svg"
                      alt="more"
                      width={24}
                      height={24}
                      priority
                      onClick={() => setSelectedNotification(notif)}
                    />
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <ModalMenu
        isOpen={!!selectedNotification}
        onClose={() => setSelectedNotification(null)}
        options={
          selectedNotification ? getMenuOptions(selectedNotification) : []
        }
      />
    </div>
  );
}
