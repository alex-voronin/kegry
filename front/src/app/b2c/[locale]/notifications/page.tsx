'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ReportDialog } from '@/components/ui/report-dialog';
import { Spinner } from '@/components/ui/spinner';
import { ListItem } from '@/components/ui/list-item';
import { useInView } from 'react-intersection-observer';
import { useViewportItemCount } from '@/hooks/use-viewport-item-count';
import {
  useNotificationsStore,
  NotificationDto,
  isNotificationExpired,
} from '@/app/b2c/[locale]/notifications/use-notifications-store';
import { formatNotificationDate } from '@/lib/date';
import { toast } from 'react-toastify';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import styles from './notifications.module.css';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import { CalendarClock } from 'lucide-react';
import { useSafeBack } from '@/hooks/use-safe-back';

export default function NotificationsScreen() {
  const router = useRouter();
  const t = useTranslations('screens.notifications');
  const [selectedNotification, setSelectedNotification] =
    useState<NotificationDto | null>(null);
  const [notificationToReport, setNotificationToReport] =
    useState<NotificationDto | null>(null);
  const { safeBack } = useSafeBack();

  const { ref: pageBottomRef, inView } = useInView({
    threshold: 0,
    rootMargin: '200px', // Trigger slightly before reaching the bottom
  });

  const limit = useViewportItemCount({
    itemHeight: 72,
    reservedHeight: 60,
    buffer: 4,
  });

  const {
    notifications,
    isLoading,
    hasMore,
    fetchNotifications,
    acceptInvite,
    deleteNotification,
    blockUser,
    reset,
    hasError,
  } = useNotificationsStore();

  useEffect(() => {
    return () => {
      // Clear store when leaving the page so it refreshes when reopened.
      reset();
    };
  }, [reset]);

  useEffect(() => {
    // Initial load
    if (notifications.length === 0 && !isLoading && hasMore && !hasError) {
      const loadNotifications = async () => {
        try {
          await fetchNotifications(0, limit);
        } catch (err: any) {
          toast.error(err.message || t('toast.loadFailed'));
        }
      };
      loadNotifications();
    }
  }, [fetchNotifications, t, notifications.length, isLoading, hasMore]);

  useEffect(() => {
    // Load more when scrolled to the bottom
    if (
      inView &&
      hasMore &&
      !isLoading &&
      !hasError &&
      notifications.length > 0
    ) {
      const loadNextPage = async () => {
        try {
          // The current length is the offset for the next page
          await fetchNotifications(notifications.length, limit);
        } catch (err: any) {
          toast.error(err.message || t('toast.loadFailed'));
        }
      };
      loadNextPage();
    }
  }, [
    inView,
    hasMore,
    isLoading,
    notifications.length,
    fetchNotifications,
    t,
    limit,
  ]);

  const handleAccept = async (notif: NotificationDto) => {
    if (!notif.fromUserId) return;
    if (isNotificationExpired(notif.expiresAt)) {
      toast.error(t('toast.expired'));
      return;
    }
    try {
      await acceptInvite(notif.fromUserId);
      toast.success(t('toast.accepted'));
    } catch (err: any) {
      toast.error(err.message || t('toast.acceptFailed'));
    }
  };

  const handleBlock = async (
    notif: NotificationDto,
    reportMessage?: string,
  ) => {
    if (!notif.fromUserId) return;
    try {
      await blockUser(notif.fromUserId, reportMessage);
      toast.success(t('toast.blocked'));
    } catch (err: any) {
      toast.error(err.message || t('toast.blockFailed'));
    }
  };

  const handleDelete = async (notif: NotificationDto) => {
    try {
      await deleteNotification(notif.id);
    } catch (err: any) {
      toast.error(err.message || t('toast.deleteFailed'));
    }
  };

  const handleApproveJoinRequest = async (notif: NotificationDto) => {
    if (!notif.linkedEntityId || !notif.fromUserId) return;
    try {
      await apiClient.post(
        API_ENDPOINTS.EVENTS.APPROVE_PARTICIPANT(
          notif.linkedEntityId,
          notif.fromUserId,
        ),
      );
      toast.success(t('toast.accepted'));
    } catch (err: any) {
      rethrowApiError(err, t('toast.acceptFailed'));
    }
  };

  const getMenuOptions = (notif: NotificationDto) => {
    const options = [];

    if (notif.fromUserId) {
      options.push({
        icon: '/assets/friends-and-foes/user-profile-icon.svg',
        label: t('menu.viewProfile'),
        onClick: () => {
          router.push(PAGE_ROUTES.USER_PROFILE(notif.fromUserId!));
          setSelectedNotification(null);
        },
      });
    }

    if (notif.linkedEntityId) {
      options.push({
        icon: <CalendarClock size={24} className="text-white" />,
        label: t('menu.viewEvent'),
        onClick: () => {
          router.push(PAGE_ROUTES.MEETUP_DETAIL(notif.linkedEntityId!));
          setSelectedNotification(null);
        },
      });
    }

    if (
      notif.type === 'UserRequestedApprove' &&
      notif.linkedEntityId &&
      notif.fromUserId &&
      !isNotificationExpired(notif.expiresAt)
    ) {
      options.push({
        icon: '/assets/notifications/accept-icon.webp',
        label: t('menu.accept'),
        onClick: () => {
          handleApproveJoinRequest(notif);
          setSelectedNotification(null);
        },
      });
    }

    if (
      !isNotificationExpired(notif.expiresAt) &&
      notif.fromUserId &&
      notif.type !== 'UserRequestedApprove'
    ) {
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
        icon: '/assets/common/block-icon.svg',
        label: t('menu.blockAndReport'),
        onClick: () => {
          setNotificationToReport(notif);
          setSelectedNotification(null);
        },
        isDestructive: true,
      });
    }
    options.push({
      icon: '/assets/common/delete-icon.svg',
      label: t('menu.delete'),
      onClick: () => {
        handleDelete(notif);
        setSelectedNotification(null);
      },
      isDestructive: true,
    });
    return options;
  };

  return (
    <div className="flex flex-1 flex-col">
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={() => safeBack(PAGE_ROUTES.MAP)}
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
              const expired = isNotificationExpired(notif.expiresAt);
              return (
                <ListItem
                  key={notif.id}
                  dimmed={expired}
                  className={
                    notif.fromUserId || notif.linkedEntityId
                      ? 'cursor-pointer'
                      : ''
                  }
                  onClick={() => {
                    if (notif.fromUserId || notif.linkedEntityId) {
                      setSelectedNotification(notif);
                    }
                  }}
                  avatarUrl={
                    notif.avatarImageUrl ||
                    '/assets/common/avatar-placeholder.webp'
                  }
                  avatarAlt="Profile"
                  title={notif.text}
                  subtitle={
                    notif.type === 'Invite'
                      ? formatNotificationDate(notif.expiresAt, t)
                      : undefined
                  }
                  rightEl={
                    <>
                      {!expired &&
                        notif.fromUserId &&
                        notif.type !== 'UserRequestedApprove' && (
                          <Icon
                            src="/assets/notifications/accept-icon.webp"
                            alt="accept"
                            width={24}
                            height={24}
                            className="object-fill"
                            priority
                            stopPropagation
                            onClick={() => handleAccept(notif)}
                          />
                        )}
                      <Icon
                        src="/assets/common/more-icon.svg"
                        alt="more"
                        width={24}
                        height={24}
                        priority
                        stopPropagation
                        onClick={() => setSelectedNotification(notif)}
                      />
                    </>
                  }
                />
              );
            })}
            {hasMore && (
              <div ref={pageBottomRef} className="flex justify-center py-4">
                <Spinner />
              </div>
            )}
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

      <ReportDialog
        isOpen={!!notificationToReport}
        onClose={() => setNotificationToReport(null)}
        onConfirm={(message) => {
          if (notificationToReport) {
            handleBlock(notificationToReport, message);
          }
        }}
      />
    </div>
  );
}
