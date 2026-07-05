'use client';

import React, { useState, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter, usePathname } from 'next/navigation';
import {
  X,
  User,
  CalendarClock,
  MapPinned,
  Sticker,
  MapPin,
  Bell,
  ScanQrCode,
  Menu,
  AlertCircle,
} from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';
import { usePendingApprovalsStore } from './use-pending-approvals-store';
import styles from './Sidebar.module.css';

export const useSidebar = () => {
  const [isOpen, setIsOpen] = useState(false);

  const open = useCallback(() => setIsOpen(true), []);
  const close = useCallback(() => setIsOpen(false), []);
  const toggle = useCallback(() => setIsOpen((prev) => !prev), []);

  return { isOpen, open, close, toggle };
};

interface SidebarIconProps {
  onClick: () => void;
  className?: string;
  size?: number;
}

export const SidebarIcon: React.FC<SidebarIconProps> = ({
  onClick,
  className = '',
  size = 28,
}) => {
  return (
    <div
      className={`flex h-10 w-10 cursor-pointer items-center justify-center rounded-full hover:bg-white/10 ${className}`}
      onClick={onClick}
    >
      <Menu size={size} className="text-primary" />
    </div>
  );
};

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
  onCheckIn: () => void;
  showPermissionsBadge?: boolean;
}

export const Sidebar: React.FC<SidebarProps> = ({
  isOpen,
  onClose,
  onCheckIn,
  showPermissionsBadge = false,
}) => {
  const t = useTranslations('screens.sidebar');
  const router = useRouter();
  const pathname = usePathname();
  const pendingTotal = usePendingApprovalsStore((s) => s.total);

  const navItems = [
    {
      label: t('profile'),
      href: PAGE_ROUTES.PROFILE,
      icon: (
        <span className="relative">
          <User size={24} />
          {showPermissionsBadge && (
            <AlertCircle
              size={14}
              strokeWidth={3}
              className="text-alert-vibrant absolute -top-1 -right-1"
            />
          )}
        </span>
      ),
    },
    {
      label: t('checkIn'),
      onClick: () => {
        onClose();
        onCheckIn();
      },
      icon: <ScanQrCode size={24} />,
    },
    {
      label: t('myEvents'),
      href: PAGE_ROUTES.MY_EVENTS,
      icon: (
        <span className="relative">
          <CalendarClock size={24} />
          {pendingTotal > 0 && (
            <span className="absolute -top-1 -right-1 rounded-full bg-[var(--color-alert-vibrant)] px-1 text-xs leading-tight text-white">
              {pendingTotal > 99 ? '99+' : pendingTotal}
            </span>
          )}
        </span>
      ),
    },
    {
      label: t('map'),
      href: PAGE_ROUTES.MAP,
      icon: <MapPinned size={24} />,
    },
    {
      label: t('feed'),
      href: PAGE_ROUTES.FEED,
      icon: <Sticker size={24} />,
    },
    {
      label: t('venues'),
      href: PAGE_ROUTES.VENUES,
      icon: <MapPin size={24} />,
    },
    {
      label: t('notifications'),
      href: PAGE_ROUTES.NOTIFICATIONS,
      icon: <Bell size={24} />,
    },
  ];

  const handleNavClick = (item: (typeof navItems)[0]) => {
    if (item.onClick) {
      item.onClick();
    } else if (item.href) {
      onClose();
      router.push(item.href);
    }
  };

  return (
    <>
      <div
        className={`${styles.drawerOverlay} ${isOpen ? styles.drawerOverlayOpen : ''}`}
        onClick={onClose}
      />
      <aside className={`${styles.drawer} ${isOpen ? styles.drawerOpen : ''}`}>
        <div className={styles.drawerHeader}>
          <div />
          <button
            className={styles.drawerCloseButton}
            onClick={onClose}
            aria-label="Close menu"
          >
            <X size={28} />
          </button>
        </div>

        <nav className={styles.drawerNav}>
          {navItems.map((item, index) => {
            const isActive = item.href ? pathname.endsWith(item.href) : false;
            return (
              <div
                key={item.href || index}
                className={`${styles.drawerNavLink} ${isActive ? styles.drawerNavLinkActive : ''}`}
                onClick={() => handleNavClick(item)}
              >
                {item.icon}
                <span>{item.label}</span>
              </div>
            );
          })}
        </nav>
      </aside>
    </>
  );
};
