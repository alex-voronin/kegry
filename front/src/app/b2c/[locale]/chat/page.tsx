'use client';

import {
  useState,
  useEffect,
  useCallback,
  useRef,
  useLayoutEffect,
} from 'react';
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
import { useChatStore } from './use-chat-store';
import { useFeedStore } from '@/app/b2c/[locale]/feed/use-feed-store';
import { useFcmStore } from '@/lib/fcm/store';
import { FcmPayload, NotificationType } from '@/lib/fcm/types';
import EmojiPicker, {
  EmojiClickData,
  Theme,
  EmojiStyle,
} from 'emoji-picker-react';
import * as linkify from 'linkifyjs';
import { cn } from '@/lib/utils';

const LOW_MESSAGE_WARNING_THRESHOLD = 10;
const END_MEETING_PROMPT_COOLDOWN = 8 * 60 * 60 * 1000; // 8 hours
const END_MEETING_PROMPT_CLOSED_AT_KEY = 'end_meeting_prompt_closed_at';

const renderMessageText = (text: string) => {
  const links = linkify.find(text, 'url');
  if (links.length === 0) return text;

  const elements = [];
  let lastIndex = 0;

  links.forEach((link, i) => {
    if (link.start > lastIndex) {
      elements.push(text.substring(lastIndex, link.start));
    }
    const href = link.href.startsWith('http')
      ? link.href
      : `https://${link.href}`;
    elements.push(
      <a
        key={`link-${i}`}
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        className="wrap-break-words underline"
        onClick={(e) => e.stopPropagation()}
      >
        {link.value}
      </a>,
    );
    lastIndex = link.end;
  });

  if (lastIndex < text.length) {
    elements.push(text.substring(lastIndex));
  }

  return elements;
};

export default function ChatScreen() {
  const t = useTranslations('screens.chat');
  const [message, setMessage] = useState('');
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isTimerInfoOpen, setIsTimerInfoOpen] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [remainingTimeMs, setRemainingTimeMs] = useState<number | null>(null);
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [maxEmojiPickerHeight, setMaxEmojiPickerHeight] = useState<number>(350);
  const [isLowMessageWarningOpen, setIsLowMessageWarningOpen] = useState(false);
  const [isFirstTimePopupOpen, setIsFirstTimePopupOpen] = useState(false);
  const router = useRouter();

  const emojiPickerRef = useRef<HTMLDivElement>(null);
  const emojiButtonRef = useRef<HTMLDivElement>(null);
  const footerRef = useRef<HTMLElement>(null);
  const headerRef = useRef<HTMLElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        showEmojiPicker &&
        emojiPickerRef.current &&
        !emojiPickerRef.current.contains(event.target as Node) &&
        emojiButtonRef.current &&
        !emojiButtonRef.current.contains(event.target as Node)
      ) {
        setShowEmojiPicker(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [showEmojiPicker]);

  const {
    chat,
    isLoading,
    fetchChat,
    sendMessage,
    deleteChat,
    showEndMeetingPrompt,
    setShowEndMeetingPrompt,
    refreshChat,
    addMessage,
  } = useChatStore();

  const messageListRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = useCallback((behavior: ScrollBehavior = 'smooth') => {
    const el = messageListRef.current;
    if (el) {
      el.scrollTo({ top: el.scrollHeight, behavior });
    }
  }, []);

  // Scroll to bottom when messages change
  useLayoutEffect(() => {
    if (chat?.messages) {
      // Use instant scroll for initial load, smooth for new messages
      const behavior = chat.messages.length > 0 ? 'auto' : 'smooth';
      scrollToBottom(behavior);
    }
  }, [chat?.messages, scrollToBottom]);

  // Scroll on mount
  useEffect(() => {
    scrollToBottom('auto');
  }, [scrollToBottom]);

  // Scroll to bottom on viewport resize (e.g. keyboard open)
  // and update emoji picker max height
  useEffect(() => {
    const handleResize = () => {
      scrollToBottom('auto');

      if (window.visualViewport) {
        const viewportHeight = window.visualViewport.height;
        const headerHeight = headerRef.current?.offsetHeight || 80;
        const footerHeight = footerRef.current?.offsetHeight || 60;
        const availableHeight =
          viewportHeight - headerHeight - footerHeight - 20;
        setMaxEmojiPickerHeight(Math.max(200, availableHeight));
      }
    };

    if (window.visualViewport) {
      window.visualViewport.addEventListener('resize', handleResize);
      // Also trigger on mount/load to get initial keyboard-less height
      handleResize();
    }
    window.addEventListener('resize', handleResize);

    return () => {
      if (window.visualViewport) {
        window.visualViewport.removeEventListener('resize', handleResize);
      }
      window.removeEventListener('resize', handleResize);
    };
  }, [scrollToBottom]);

  const { registerForegroundHandler, unregisterForegroundHandler } =
    useFcmStore();

  useEffect(() => {
    const loadChat = async () => {
      try {
        await fetchChat();
      } catch (err: any) {
        if (err.code === 'NotFound') {
          router.replace(PAGE_ROUTES.FEED);
          return;
        }
        toast.error(err.message || t('toast.loadFailed'));
      }
    };
    loadChat();
  }, [fetchChat, t]);

  // Handle automatic "end meeting" prompt based on participantsMet and cooldown period
  useEffect(() => {
    if (chat?.participantsMet) {
      const lastClosedAt = localStorage.getItem(
        END_MEETING_PROMPT_CLOSED_AT_KEY,
      );
      const now = Date.now();

      if (
        !lastClosedAt ||
        now - parseInt(lastClosedAt, 10) > END_MEETING_PROMPT_COOLDOWN
      ) {
        setShowEndMeetingPrompt(true);
      }
    }
  }, [chat?.participantsMet, setShowEndMeetingPrompt]);

  // Register FCM handler for showEndMeetingSuggestion and chatMessageReceived
  useEffect(() => {
    const handleFcmMessage = (payload: FcmPayload) => {
      const data = payload?.data;
      if (!data) return;

      const type = data.type;

      if (type === NotificationType.SHOW_END_MEETING_SUGGESTION) {
        setShowEndMeetingPrompt(true);
      }

      if (type === NotificationType.CHAT_MESSAGE_RECEIVED) {
        if (!data.messageText || !data.createdAt || !data.chatMessageId) {
          refreshChat();
          return;
        }

        const currentChat = useChatStore.getState().chat;
        const messages = currentChat?.messages || [];
        const lastMessage = messages[messages.length - 1];

        if (
          lastMessage &&
          new Date(data.createdAt) < new Date(lastMessage.createdAt)
        ) {
          console.warn('[Chat] Out-of-order message detected, reloading chat');
          refreshChat();
          return;
        }

        addMessage({
          id: data.chatMessageId,
          messageText: data.messageText,
          createdAt: data.createdAt,
          isMine: false,
        });
      }
    };

    registerForegroundHandler(handleFcmMessage);
    return () => unregisterForegroundHandler(handleFcmMessage);
  }, [
    registerForegroundHandler,
    unregisterForegroundHandler,
    setShowEndMeetingPrompt,
    refreshChat,
    addMessage,
  ]);

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

    // Check for "first time chat" popup
    const shownKey = 'chat_intro_shown';
    const hasShown = window.localStorage.getItem(shownKey);
    if (!hasShown) {
      setIsFirstTimePopupOpen(true);
      window.localStorage.setItem(shownKey, 'true');
    }

    setRemainingTimeMs(calculateRemainingTimeMs());

    const interval = setInterval(() => {
      setRemainingTimeMs(calculateRemainingTimeMs());
    }, 10000);

    return () => clearInterval(interval);
  }, [chat?.id, chat?.expiresAt, calculateRemainingTimeMs]);

  // Auto-cancel meeting when timer expires
  useEffect(() => {
    if (remainingTimeMs === 0) {
      handleEndMeeting();
    }
  }, [remainingTimeMs]);

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
      if (remainingMessages === LOW_MESSAGE_WARNING_THRESHOLD + 1) {
        setIsLowMessageWarningOpen(true);
      }
    } catch (err: any) {
      toast.error(err.message || t('toast.sendFailed'));
    } finally {
      setIsSending(false);
    }
  };

  const handleEndMeeting = async () => {
    setShowEndMeetingPrompt(false);
    setIsMenuOpen(false);
    localStorage.removeItem(END_MEETING_PROMPT_CLOSED_AT_KEY);
    useFeedStore.setState({ isInMeeting: false });
    try {
      await deleteChat();
      router.push(PAGE_ROUTES.FEED);
    } catch (err: any) {
      toast.error(err.message || t('toast.cancelFailed'));
    }
  };

  const handleDismissEndMeetingPrompt = () => {
    setShowEndMeetingPrompt(false);
    localStorage.setItem(
      END_MEETING_PROMPT_CLOSED_AT_KEY,
      Date.now().toString(),
    );
  };

  const onEmojiClick = (emojiData: EmojiClickData) => {
    setMessage((prev) => prev + emojiData.emoji);
  };

  const menuOptions = [
    {
      icon: '/assets/chat/cancel-icon.svg',
      label: t('menu.cancel'),
      onClick: handleEndMeeting,
    },
    {
      icon: '/assets/venue/venue-icon.svg',
      label: t('menu.venues'),
      onClick: () => {
        router.push(PAGE_ROUTES.VENUES);
      },
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
              onClick={() => router.replace(PAGE_ROUTES.FEED)}
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
        ref={headerRef}
        className="min-h-[80px]"
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            priority
            onClick={() =>
              router.push(`${PAGE_ROUTES.FEED}?auto-chat-redirect=false`)
            }
          />
        }
        title={
          <div
            className={styles.titleWrapper}
            onClick={() =>
              router.push(PAGE_ROUTES.USER_PROFILE(chat.participant.id))
            }
          >
            <div className={styles.avatarCircle}>
              <Image
                src={
                  chat.participant.avatarUrl ||
                  '/assets/common/avatar-placeholder.webp'
                }
                alt="avatar"
                fill
                className="object-cover"
              />
            </div>
            <p className={styles.userName}>{participantName}</p>
          </div>
        }
        right={
          <div className={styles.headerActionsWrapper}>
            <Icon
              src="/assets/common/beer-mug.webp"
              alt="status"
              width={36}
              height={36}
              priority
              onClick={() => setIsTimerInfoOpen(true)}
            />
            <span className={styles.timer}>
              {formatRemainingTime(remainingTimeMs)}
            </span>
            <Icon
              src="/assets/common/more-icon.svg"
              alt="more"
              width={28}
              height={28}
              priority
              onClick={() => setIsMenuOpen(true)}
            />
          </div>
        }
      />

      <div ref={messageListRef} className={styles.messageList}>
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
              {renderMessageText(msg.messageText)}
            </div>
            <span className={styles.timestamp}>
              {formatTime(msg.createdAt)}
            </span>
          </div>
        ))}
      </div>

      <footer ref={footerRef} className={styles.footer}>
        {showEmojiPicker && (
          <div ref={emojiPickerRef} className={styles.emojiPickerWrapper}>
            <EmojiPicker
              onEmojiClick={onEmojiClick}
              theme={Theme.DARK}
              lazyLoadEmojis={true}
              emojiStyle={EmojiStyle.NATIVE}
              width={300}
              height={maxEmojiPickerHeight}
              previewConfig={{
                showPreview: false,
              }}
            />
          </div>
        )}
        <div ref={emojiButtonRef} className="flex">
          <Icon
            className={styles.footerIcon}
            src="/assets/chat/smiley-icon.svg"
            alt="emoji"
            width={24}
            height={24}
            priority
            onClick={() => setShowEmojiPicker((prev) => !prev)}
          />
        </div>
        <textarea
          value={message}
          onChange={(e) => onMessageChange(e)}
          onKeyDown={(e) => {
            if (
              (e.metaKey || e.ctrlKey) &&
              e.key === 'Enter' &&
              remainingMessages > 0
            ) {
              e.preventDefault();
              handleSendMessage();
            }
          }}
          className={styles.input}
          placeholder={t('messagePlaceholder', {
            remaining: remainingMessages,
            total: maxMessages,
          })}
          rows={1}
        />
        <div
          className={cn(
            styles.sendButton,
            remainingMessages <= 0 ? 'opacity-50' : 'hover:opacity-80',
          )}
          onClick={remainingMessages > 0 ? handleSendMessage : undefined}
        >
          <Image
            className="ml-1"
            src="/assets/chat/send-icon.svg"
            alt="send"
            width={21}
            height={21}
            priority
          />
        </div>
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

      <ModalDialog
        isOpen={showEndMeetingPrompt}
        onClose={handleDismissEndMeetingPrompt}
        message={t('endMeetingDialog.message')}
        primaryButtonText={t('endMeetingDialog.no')}
        secondaryButtonText={t('endMeetingDialog.yes')}
        onPrimaryAction={handleDismissEndMeetingPrompt}
        onSecondaryAction={handleEndMeeting}
      />

      <ModalDialog
        isOpen={isLowMessageWarningOpen}
        onClose={() => setIsLowMessageWarningOpen(false)}
        message={t('lowMessageWarning.message', {
          count: LOW_MESSAGE_WARNING_THRESHOLD,
        })}
        primaryButtonText={t('lowMessageWarning.okButton')}
        onPrimaryAction={() => setIsLowMessageWarningOpen(false)}
      />

      <ModalDialog
        isOpen={isFirstTimePopupOpen}
        onClose={() => setIsFirstTimePopupOpen(false)}
        message={t('firstTimePopup.message', {
          count: remainingMessages,
        })}
        primaryButtonText={t('firstTimePopup.okButton')}
        onPrimaryAction={() => setIsFirstTimePopupOpen(false)}
      />
    </div>
  );
}
