import Image from 'next/image';
import styles from '../feed.module.css';
import { NpcDto } from '@/store/use-npc-store';

export function NpcFeedBody({
  npc,
  monolog,
}: {
  npc: NpcDto;
  monolog: string | null;
}) {
  return (
    <>
      <div>
        <Image
          src={npc.avatarUrl}
          alt={npc.name}
          fill
          className={styles.profileImage}
          priority
        />
        <div className={styles.imageOverlay} />
      </div>

      <div className={styles.npcInfo}>
        <h1 className={styles.npcName}>{npc.name}</h1>
      </div>

      {monolog && (
        <div className={styles.monologBubble}>
          <p className={styles.monologText}>{monolog}</p>
          <div className={styles.monologTail} />
        </div>
      )}
    </>
  );
}

export function NpcFeedFooter({
  onInvite,
  onDismiss,
  hasMonolog,
}: {
  onInvite: () => void;
  onDismiss: () => void;
  hasMonolog: boolean;
}) {
  return (
    <footer className={styles.actions}>
      <button
        className={styles.actionButton}
        onClick={(e) => {
          e.stopPropagation();
          onDismiss();
        }}
      >
        <Image
          className="pointer-events-none"
          src="/assets/feed/skip-icon.svg"
          alt="dismiss"
          width={24}
          height={24}
          priority
        />
      </button>
      {!hasMonolog && (
        <button
          className={styles.actionPrimary}
          onClick={(e) => {
            e.stopPropagation();
            onInvite();
          }}
        >
          <Image
            className="pointer-events-none translate-x-[2px]"
            src="/assets/feed/invite-icon.webp"
            alt="invite"
            width={35}
            height={49}
            priority
          />
        </button>
      )}
    </footer>
  );
}
