'use client';

import { useState } from 'react';
import Image from 'next/image';
import { useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import styles from './chat.module.css';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { ModalMenu } from '@/components/ui/modal-menu';

export default function ChatScreen() {
  const t = useTranslations('screens.chat');
  const [message, setMessage] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const router = useRouter();

  const onMessageChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const el = e.target;
    setMessage(el.value);

    el.style.height = 'inherit';
    el.style.height = `${el.scrollHeight}px`;
  };

  const menuOptions = [
    {
      icon: '/assets/chat/cancel-icon.svg',
      label: t('menu.cancel'),
      onClick: () => router.push('/finder'),
    },
  ];

  const messages = [
    {
      id: 1,
      text: "Hi Gena! I'm doing well, thanks. Can't wait for the weekend!",
      sender: 'other',
      time: '10:30 AM',
    },
    {
      id: 2,
      text: 'I know, right? Weekend plans are the best. Any exciting plans on your end?',
      sender: 'me',
      time: '10:31 AM',
    },
  ];

  return (
    <div className={styles.wrapper}>
      <Header
        className="min-h-[80px]"
        left={
          <Icon
            src="/assets/finder/profile-icon.svg"
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
            />
            <span className={styles.timer}>47:59</span>
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
            src="/assets/finder/profile-image.png"
            alt="avatar"
            fill
            className="object-cover"
          />
        </div>
        <p className={styles.userName}>Ivan Laptev</p>
      </div>

      <div className={styles.messageList}>
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`${styles.messageRow} ${
              msg.sender === 'me'
                ? styles.messageRowRight
                : styles.messageRowLeft
            }`}
          >
            <div
              className={`${styles.messageBubble} ${
                msg.sender === 'me' ? styles.bubbleRight : styles.bubbleLeft
              }`}
            >
              {msg.text}
            </div>
            <span className={styles.timestamp}>{msg.time}</span>
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
        />
      </footer>

      <ModalMenu
        isOpen={isMenuOpen}
        onClose={() => setIsMenuOpen(false)}
        options={menuOptions}
      />
    </div>
  );
}
