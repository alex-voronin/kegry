'use client';

import { useState, useEffect, useCallback } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { toast } from 'react-toastify';
import styles from './chat.module.css';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { Spinner } from '@/components/ui/spinner';
import { PAGE_ROUTES } from '@/lib/routes';
import { useChatStore } from '@/store/use-chat-store';

export default function ChatScreen() {
  const t = useTranslations('screens.chat');
  const [message, setMessage] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isTimerInfoOpen, setIsTimerInfoOpen] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [remainingTimeMs, setRemainingTimeMs] = useState<number | null>(null);
  const router = useRouter();

  const { chat, isLoading, fetchChat, sendMessage, deleteChat } =
    useChatStore();

  useEffect(() => {
    fetchChat();
  }, [fetchChat]);

  const calculateRemainingTimeMs = useCallback(() => {
    if (!chat?.expiresAt) return null;

    const now = new Date().getTime();
    const expiresAt = new Date(chat.expiresAt).getTime();
    const diff = expiresAt - now;

    return Math.max(0, diff);
  }, [chat?.expiresAt]);

  const formatRemainingTime = (ms: number | null): string => {
    if (ms === null) return '--:--';
    if (ms <= 0) return '00:00';

    const hours = Math.floor(ms / (1000 * 60 * 60));
    const minutes = Math.floor((ms % (1000 * 60 * 60)) / (1000 * 60));

    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
  };

  useEffect(() => {
    if (!chat?.expiresAt) return;

    setRemainingTimeMs(calculateRemainingTimeMs());

    const interval = setInterval(() => {
      setRemainingTimeMs(calculateRemainingTimeMs());
    }, 10000);

    return () => clearInterval(interval);
  }, [chat?.expiresAt, calculateRemainingTimeMs]);

  const onMessageChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const el = e.target;
    setMessage(el.value);

    el.style.height = 'inherit';
    el.style.height = `${el.scrollHeight}px`;
  };

  const handleSendMessage = async () => {
    if (!message.trim() || isSending) return;

    setIsSending(true);
    try {
      await sendMessage(message.trim());
      setMessage('');
    } catch (err: any) {
      toast.error(err.message || t('toast.sendFailed'));
    } finally {
      setIsSending(false);
    }
  };

  const handleCancelMeeting = async () => {
    setIsMenuOpen(false);
    try {
      await deleteChat();
      router.push(PAGE_ROUTES.FEED);
    } catch (err: any) {
      toast.error(err.message || t('toast.cancelFailed'));
    }
  };

  const menuOptions = [
    {
      icon: '/assets/chat/cancel-icon.svg',
      label: t('menu.cancel'),
      onClick: handleCancelMeeting,
    },
  ];

  const sentMessagesCount = chat?.messages.filter((m) => m.isMine).length ?? 0;
  const maxMessages = chat?.maxMessagesPerUser ?? 0;
  const remainingMessages = Math.max(0, maxMessages - sentMessagesCount);

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    const today = new Date();
    const isToday =
      date.getDate() === today.getDate() &&
      date.getMonth() === today.getMonth() &&
      date.getFullYear() === today.getFullYear();

    const time = date.toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
    });

    if (isToday) {
      return time;
    }

    const dateStr = date.toLocaleDateString([], {
      month: 'numeric',
      day: 'numeric',
    });
    return `${dateStr}, ${time}`;
  };

  if (isLoading) {
    return (
      <div className={styles.wrapper}>
        <div className={styles.loadingWrapper}>
          <Spinner />
        </div>
      </div>
    );
  }

  if (!chat) {
    return (
      <div className={styles.wrapper}>
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
        />
        <div className={styles.emptyState}>
          <span>{t('noActiveChat')}</span>
        </div>
      </div>
    );
  }

  const participantName = `${chat.participant.firstName} ${chat.participant.lastName}`;

  return (
    <div className={styles.wrapper}>
      <Header
        className="min-h-[80px]"
        left={
          <Icon
            src="/assets/common/profile-icon.svg"
            alt="profile"
            width={30}
            height={32}
            priority
          />
        }
        right={
          <div className={styles.headerActionsWrapper}>
            <Icon
              src="/assets/chat/mug-icon.png"
              alt="status"
              width={36}
              height={38}
              priority
              onClick={() => setIsTimerInfoOpen(true)}
            />
            <span className={styles.timer}>
              {formatRemainingTime(remainingTimeMs)}
            </span>
            <Icon
              src="/assets/chat/more-icon.svg"
              alt="more"
              width={28}
              height={28}
              priority
              onClick={() => setIsMenuOpen(true)}
            />
          </div>
        }
      />

      <div className={styles.titleWrapper}>
        <div className={styles.avatarCircle}>
          <Image
            src={
              chat.participant.avatarUrl ||
              '/assets/common/avatar-placeholder.svg'
            }
            alt="avatar"
            fill
            className="object-cover"
          />
        </div>
        <p className={styles.userName}>{participantName}</p>
      </div>

      <div className={styles.messageList}>
        {chat.messages.map((msg) => (
          <div
            key={msg.id}
            className={`${styles.messageRow} ${
              msg.isMine ? styles.messageRowRight : styles.messageRowLeft
            }`}
          >
            <div
              className={`${styles.messageBubble} ${
                msg.isMine ? styles.bubbleRight : styles.bubbleLeft
              }`}
            >
              {msg.messageText}
            </div>
            <span className={styles.timestamp}>
              {formatTime(msg.createdAt)}
            </span>
          </div>
        ))}
      </div>

      <footer className={styles.footer}>
        <Icon
          className={styles.footerIcon}
          src="/assets/chat/smiley-icon.svg"
          alt="emoji"
          width={24}
          height={24}
          priority
        />
        <textarea
          value={message}
          onChange={(e) => onMessageChange(e)}
          className={styles.input}
          placeholder={t('messagePlaceholder', {
            remaining: remainingMessages,
            total: maxMessages,
          })}
          rows={1}
        />
        <Icon
          className={styles.footerIcon}
          src="/assets/chat/send-icon.svg"
          alt="send"
          width={28}
          height={28}
          priority
          disabled={remainingMessages <= 0}
          onClick={handleSendMessage}
        />
      </footer>

      <ModalMenu
        isOpen={isMenuOpen}
        onClose={() => setIsMenuOpen(false)}
        options={menuOptions}
      />

      <ModalDialog
        isOpen={isTimerInfoOpen}
        onClose={() => setIsTimerInfoOpen(false)}
        message={t('timerInfoDialog.title')}
        primaryButtonText={t('timerInfoDialog.okButton')}
        onPrimaryAction={() => {}}
      />
    </div>
  );
}
