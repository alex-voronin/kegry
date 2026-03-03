'use client';

import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  DialogTitle,
  Transition,
  TransitionChild,
} from '@headlessui/react';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from '@/components/ui/button';
import styles from './share-fallback-dialog.module.css';

interface ShareFallbackDialogProps {
  isOpen: boolean;
  onClose: () => void;
  url: string;
}

export function ShareFallbackDialog({
  isOpen,
  onClose,
  url,
}: ShareFallbackDialogProps) {
  const t = useTranslations('screens.friendsAndFoes.shareFallback');

  return (
    <Transition show={isOpen}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <TransitionChild
          enter="opacity-0"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="opacity-100"
          leaveFrom="opacity-0"
          leaveTo="opacity-0"
        >
          <DialogBackdrop className={styles.backdrop} />
        </TransitionChild>

        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <TransitionChild
            enter="transition duration-300 ease-out"
            enterFrom="scale-95 opacity-0"
            enterTo="scale-100 opacity-100"
            leave="transition duration-200 ease-in"
            leaveFrom="scale-100 opacity-100"
            leaveTo="scale-95 opacity-0"
          >
            <DialogPanel className={styles.modalContainer}>
              <DialogTitle className={styles.title}>{t('title')}</DialogTitle>

              <p className={styles.instruction}>{t('instruction')}</p>

              <div className={styles.linkWrapper}>
                <code className={styles.link}>{url}</code>
              </div>

              <Button
                variant={ButtonVariant.Default}
                className={styles.button}
                onClick={onClose}
              >
                {t('close')}
              </Button>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
