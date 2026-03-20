import Image from 'next/image';
import { cn } from '@/lib/utils';
import { Icon } from './icon';
import styles from './list-item.module.css';

interface ListItemProps {
  avatarUrl?: string;
  avatarAlt?: string;
  title: React.ReactNode;
  subtitle: React.ReactNode;
  rightEl?: React.ReactNode;
  onClick?: () => void;
  className?: string;
  dimmed?: boolean;
  leadingIcon?: React.ReactNode;
}

export function ListItem({
  avatarUrl,
  avatarAlt = '',
  title,
  subtitle,
  rightEl,
  onClick,
  className,
  dimmed,
  leadingIcon,
}: ListItemProps) {
  return (
    <div
      className={cn(styles.item, dimmed && styles.expired, className)}
      onClick={onClick}
    >
      <div className={styles.itemLeft}>
        <div className={styles.imageWrapper}>
          {leadingIcon ? (
            <div className="flex h-full w-full items-center justify-center">
              {leadingIcon}
            </div>
          ) : (
            avatarUrl && (
              <Image
                src={avatarUrl}
                alt={avatarAlt}
                width={48}
                height={48}
                className="object-cover"
              />
            )
          )}
        </div>
        <div className={styles.body}>
          <span className={styles.title}>{title}</span>
          <div className={styles.subtitle}>{subtitle}</div>
        </div>
      </div>

      {rightEl && <div className={styles.itemRight}>{rightEl}</div>}
    </div>
  );
}
