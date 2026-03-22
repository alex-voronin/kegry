'use client';

import React, { useState, useCallback } from 'react';
import { useTranslations } from 'next-intl';
import { useRouter, usePathname } from 'next/navigation';
import { X, User, Building2, MapPin, LogOut, Menu } from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';
import { useAuthStore } from '@/lib/auth/use-auth-store';
import styles from './Sidebar.module.css';

// --- Hook ---

export const useSidebar = () => {
  const [isOpen, setIsOpen] = useState(false);

  const open = useCallback(() => setIsOpen(true), []);
  const close = useCallback(() => setIsOpen(false), []);
  const toggle = useCallback(() => setIsOpen((prev) => !prev), []);

  return { isOpen, open, close, toggle };
};

// --- Components ---

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
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen, onClose }) => {
  const t = useTranslations('screens.sidebar');
  const router = useRouter();
  const pathname = usePathname();
  const logout = useAuthStore((state) => state.logout);

  const navItems = [
    {
      label: t('profile'),
      href: PAGE_ROUTES.B2B.PROFILE,
      icon: <User size={24} />,
    },
    {
      label: t('companies'),
      href: PAGE_ROUTES.B2B.COMPANIES,
      icon: <Building2 size={24} />,
    },
    {
      label: t('venues'),
      href: PAGE_ROUTES.B2B.VENUES,
      icon: <MapPin size={24} />,
    },
  ];

  const handleNavClick = (href: string) => {
    onClose();
    router.push(href);
  };

  const handleLogout = async () => {
    onClose();
    await logout();
    router.push(PAGE_ROUTES.B2B.LOGIN());
  };

  return (
    <>
      <div
        className={`${styles.drawerOverlay} ${isOpen ? styles.drawerOverlayOpen : ''}`}
        onClick={onClose}
      />
      <aside className={`${styles.drawer} ${isOpen ? styles.drawerOpen : ''}`}>
        <button
          className={styles.drawerCloseButton}
          onClick={onClose}
          aria-label="Close menu"
        >
          <X size={28} />
        </button>

        <nav className={styles.drawerNav}>
          {navItems.map((item) => {
            const isActive = pathname.endsWith(item.href);
            return (
              <div
                key={item.href}
                className={`${styles.drawerNavLink} ${isActive ? styles.drawerNavLinkActive : ''}`}
                onClick={() => handleNavClick(item.href)}
              >
                {item.icon}
                <span>{item.label}</span>
              </div>
            );
          })}

          <div className={styles.drawerNavBottom}>
            <div className={styles.drawerNavLink} onClick={handleLogout}>
              <LogOut size={24} />
              <span>{t('logout')}</span>
            </div>
          </div>
        </nav>
      </aside>
    </>
  );
};
