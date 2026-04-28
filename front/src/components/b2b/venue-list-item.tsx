import Image from 'next/image';
import { cn } from '@/lib/utils';
import styles from './venue-list-item.module.css';

interface VenueListItemProps {
  imageUrl?: string;
  imageAlt?: string;
  title: React.ReactNode;
  rightEl?: React.ReactNode;
  onClick?: () => void;
  className?: string;
  dimmed?: boolean;
}

export function VenueListItem({
  imageUrl,
  imageAlt = '',
  title,
  rightEl,
  onClick,
  className,
  dimmed,
}: VenueListItemProps) {
  return (
    <div
      className={cn(styles.item, dimmed && styles.nonVerified, className)}
      onClick={onClick}
    >
      {imageUrl && (
        <Image src={imageUrl} alt={imageAlt} fill className={styles.image} />
      )}
      <div className={styles.overlay} />

      <div className={styles.content}>
        <div className={styles.titleWrapper}>
          <span className={styles.title}>{title}</span>
        </div>
        {rightEl && <div className={styles.itemRight}>{rightEl}</div>}
      </div>
    </div>
  );
}
