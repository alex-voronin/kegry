import { ReactNode } from 'react';
import styles from './participant-list-item.module.css';

interface Hobby {
  id: string;
  name: string;
}

interface ParticipantListItemProps {
  avatarUrl: string | null;
  firstName: string;
  lastName: string;
  userId: string;
  hobbies?: Hobby[];
  subtitle?: ReactNode;
  translateHobby?: (key: string) => string;
  rightEl?: ReactNode;
  onClick?: () => void;
}

export function ParticipantListItem({
  avatarUrl,
  firstName,
  lastName,
  userId,
  hobbies,
  subtitle,
  translateHobby,
  rightEl,
  onClick,
}: ParticipantListItemProps) {
  return (
    <div
      className={`${styles.root} ${onClick ? styles.rootClickable : ''}`}
      onClick={onClick}
    >
      <div className={styles.row}>
        {avatarUrl ? (
          <img src={avatarUrl} alt="" className={styles.avatar} />
        ) : (
          <div className={styles.avatarFallback}>
            {(firstName ?? userId).charAt(0).toUpperCase()}
          </div>
        )}
        <div className={styles.nameArea}>
          <p className={styles.name}>
            {firstName && lastName
              ? `${firstName} ${lastName}`
              : userId.slice(0, 8)}
          </p>
          {subtitle}
        </div>
        {rightEl && (
          <div className={styles.actions} onClick={(e) => e.stopPropagation()}>
            {rightEl}
          </div>
        )}
      </div>
      {hobbies && hobbies.length > 0 && (
        <div className={styles.hobbies}>
          {hobbies.map((h) => (
            <span key={h.id} className={styles.hobbyTag}>
              {(translateHobby ? translateHobby(h.name) : h.name) || h.name}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}
