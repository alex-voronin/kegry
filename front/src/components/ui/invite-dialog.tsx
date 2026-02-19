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
import { InviteExpiration } from '@/lib/invite-utils';
import { WhenSelector } from '@/app/[locale]/feed/_components/user-feed-card';
import styles from './invite-dialog.module.css';

interface InviteDialogProps {
  isOpen: boolean;
  friendName: string;
  onClose: () => void;
  onConfirm: (when: InviteExpiration) => void;
}

export function InviteDialog({
  isOpen,
  friendName,
  onClose,
  onConfirm,
}: InviteDialogProps) {
  const t = useTranslations('components.inviteDialog');
  const [selected, setSelected] = useState<InviteExpiration>(
    InviteExpiration.Today,
  );

  const handleConfirm = () => {
    onConfirm(selected);
    setSelected(InviteExpiration.Today);
    onClose();
  };

  const handleClose = () => {
    setSelected(InviteExpiration.Today);
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
              <DialogTitle className={styles.title}>
                {t('title', { name: friendName })}
              </DialogTitle>

              <div className={styles.selectorWrapper}>
                <WhenSelector
                  value={selected}
                  onChange={setSelected}
                  tFeed={t}
                  className={styles.selectorInner}
                  fullWidth
                  stretchItems
                />
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
                  variant={ButtonVariant.Default}
                  className={styles.button}
                  onClick={handleConfirm}
                >
                  {t('ok')}
                </Button>
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
