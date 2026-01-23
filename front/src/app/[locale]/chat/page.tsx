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
  const [remainingTime, setRemainingTime] = useState<string>('--:--');
  const router = useRouter();

  const { chat, isLoading, fetchChat, sendMessage, deleteChat } =
    useChatStore();

  useEffect(() => {
    fetchChat();
  }, [fetchChat]);

  const calculateRemainingTime = useCallback(() => {
    if (!chat?.expiresAt) return '--:--';

    const now = new Date().getTime();
    const expiresAt = new Date(chat.expiresAt).getTime();
    const diff = expiresAt - now;

    if (diff <= 0) {
      return '00:00';
    }

    const minutes = Math.floor(diff / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);

    return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  }, [chat?.expiresAt]);

  useEffect(() => {
    if (!chat?.expiresAt) return;

    setRemainingTime(calculateRemainingTime());

    const interval = setInterval(() => {
      const time = calculateRemainingTime();
      setRemainingTime(time);
    }, 1000);

    return () => clearInterval(interval);
  }, [chat?.expiresAt, calculateRemainingTime]);

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

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
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
            <span className={styles.timer}>{remainingTime}</span>
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
          placeholder={t('messagePlaceholder')}
          rows={1}
        />
        <Icon
          className={styles.footerIcon}
          src="/assets/chat/send-icon.svg"
          alt="send"
          width={28}
          height={28}
          priority
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
