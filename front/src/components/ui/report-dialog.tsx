'use client';

import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  DialogTitle,
  Transition,
  TransitionChild,
} from '@headlessui/react';
import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { Button, ButtonVariant } from './button';
import styles from './report-dialog.module.css';

const MAX_LENGTH = 1024;

interface ReportDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (message: string) => void;
}

export function ReportDialog({
  isOpen,
  onClose,
  onConfirm,
}: ReportDialogProps) {
  const t = useTranslations('components.reportDialog');
  const [message, setMessage] = useState('');

  const handleConfirm = () => {
    onConfirm(message);
    setMessage('');
    onClose();
  };

  const handleClose = () => {
    setMessage('');
    onClose();
  };

  return (
    <Transition show={isOpen}>
      <Dialog as="div" className="relative z-50" onClose={handleClose}>
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

        <div className="fixed inset-0 z-50 flex items-end justify-center p-4">
          <TransitionChild
            enter="translate-y-full"
            enterFrom="translate-y-full"
            enterTo="translate-y-0"
            leave="translate-y-0"
            leaveFrom="translate-y-0"
            leaveTo="translate-y-full"
          >
            <DialogPanel className={styles.modalContainer}>
              <DialogTitle className={styles.title}>{t('title')}</DialogTitle>

              <div className={styles.textareaWrapper}>
                <textarea
                  className={styles.textarea}
                  placeholder={t('placeholder')}
                  value={message}
                  maxLength={MAX_LENGTH}
                  onChange={(e) => setMessage(e.target.value)}
                />
                <span className={styles.charCounter}>
                  {MAX_LENGTH - message.length}
                </span>
              </div>

              <div className={styles.buttonGroup}>
                <Button
                  variant={ButtonVariant.Secondary}
                  className={styles.button}
                  onClick={handleClose}
                >
                  {t('cancel')}
                </Button>
                <Button
                  variant={ButtonVariant.Destructive}
                  className={styles.button}
                  onClick={handleConfirm}
                >
                  {t('confirm')}
                </Button>
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
