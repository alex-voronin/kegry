'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { useRouter } from '@/i18n/routing';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import styles from './notifications.module.css';

interface Notification {
  id: number;
  type: 'invite';
  title: string;
  subtitle: string;
}

export default function NotificationsScreen() {
  const router = useRouter();
  const t = useTranslations('screens.notifications');
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const notifications: Notification[] = [
    {
      id: 1,
      type: 'invite',
      title: t('drinkInvitation', { time: t('today') }),
      subtitle: t('todayUntil', { h: 24 }),
    },
    {
      id: 2,
      type: 'invite',
      title: t('drinkInvitation', { time: t('rightNow') }),
      subtitle: t('todayUntil', { h: 22 }),
    },
    {
      id: 3,
      type: 'invite',
      title: t('drinkInvitation', { time: t('tomorrow') }),
      subtitle: t('ago', { time: '1w' }),
    },
  ];

  const menuOptions = [
    {
      icon: '/assets/notifications/accept-icon.png',
      label: t('menu.accept'),
      onClick: () => console.log('Accept clicked'),
    },
    {
      icon: '/assets/notifications/ignore-icon.svg',
      label: t('menu.ignore'),
      onClick: () => console.log('Ignore clicked'),
    },
    {
      icon: '/assets/notifications/block-icon.svg',
      label: t('menu.block'),
      onClick: () => console.log('Block clicked'),
      isDestructive: true,
    },
  ];

  return (
    <div>
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
        <div className={styles.notificationList}>
          {notifications.map((notif) => (
            <div key={notif.id} className={styles.notificationItem}>
              <div className={styles.notificationItemLeft}>
                <div className={styles.profileImageWrapper}>
                  <Image
                    src="/assets/finder/profile-image.png"
                    alt="Profile"
                    width={48}
                    height={48}
                    className="object-cover"
                  />
                </div>
                <div className={styles.notificationBody}>
                  <span className={styles.notificationTitle}>
                    {notif.title}
                  </span>
                  <span className={styles.notificationTime}>
                    {notif.subtitle}
                  </span>
                </div>
              </div>

              <div className={styles.notificationItemRight}>
                <Icon
                  src="/assets/notifications/accept-icon.png"
                  alt="accept"
                  width={24}
                  height={24}
                  className="object-fill"
                  priority
                />
                <Icon
                  src="/assets/chat/more-icon.svg"
                  alt="more"
                  width={24}
                  height={24}
                  priority
                  onClick={() => setIsMenuOpen(true)}
                />
              </div>
            </div>
          ))}
        </div>
      </div>

      <ModalMenu
        isOpen={isMenuOpen}
        onClose={() => setIsMenuOpen(false)}
        options={menuOptions}
      />
    </div>
  );
}
