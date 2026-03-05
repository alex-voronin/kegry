import { useEffect } from 'react';
import { useTranslations } from 'next-intl';
import Image from 'next/image';
import { Icon } from '@/components/ui/icon';
import { Spinner } from '@/components/ui/spinner';
import { useMetNpcsStore } from '@/store/use-met-npcs-store';
import { toast } from 'react-toastify';
import styles from '../friends-and-foes.module.css';
import { SelectedUser } from '../types';

interface ImaginaryTabProps {
  onSelectUser: (user: SelectedUser) => void;
  onSelectCover: (cover: { name: string; coverUrl: string }) => void;
}

export function ImaginaryTab({
  onSelectUser,
  onSelectCover,
}: ImaginaryTabProps) {
  const t = useTranslations('screens.friendsAndFoes');
  const { metNpcs, isLoading, fetchMetNpcs } = useMetNpcsStore();

  useEffect(() => {
    if (metNpcs.length === 0) {
      const loadMetNpcs = async () => {
        try {
          await fetchMetNpcs();
        } catch (err: any) {
          toast.error(err.message || t('toast.fetchImaginaryFailed'));
        }
      };
      loadMetNpcs();
    }
  }, [fetchMetNpcs, metNpcs.length, t]);

  if (isLoading && metNpcs.length === 0) {
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
        <div
          key={npc.id}
          className={styles.userItem}
          onClick={() =>
            npc.coverUrl &&
            onSelectCover({
              name: npc.name,
              coverUrl: npc.coverUrl,
            })
          }
        >
          <div className={styles.userItemLeft}>
            <div className={styles.profileImageWrapper}>
              <Image
                src={npc.avatarUrl || '/assets/common/avatar-placeholder.webp'}
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

          <div
            onClick={(e) => {
              e.stopPropagation();
              onSelectUser({
                id: npc.id,
                firstName: npc.name,
                lastName: '',
                avatarUrl: npc.avatarUrl,
                coverUrl: npc.coverUrl,
                type: 'imaginary',
              });
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
}
