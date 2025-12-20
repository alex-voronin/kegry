'use client';

import { Fragment } from 'react';
import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  DialogTitle,
  Transition,
  TransitionChild,
} from '@headlessui/react';
import { Button, ButtonVariant } from './button';
import styles from './modal-dialog.module.css';

interface ModalDialogProps {
  isOpen: boolean;
  onClose: () => void;
  message: string;
  primaryButtonText: string;
  secondaryButtonText: string;
  onPrimaryAction: () => void;
  onSecondaryAction: () => void;
}

export function ModalDialog({
  isOpen,
  onClose,
  message,
  primaryButtonText,
  secondaryButtonText,
  onPrimaryAction,
  onSecondaryAction,
}: ModalDialogProps) {
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

        <div className="fixed inset-0 z-50 flex items-end justify-center p-4">
          <TransitionChild
            as={Fragment}
            enter={styles.modalEnter}
            enterFrom={styles.modalEnter}
            enterTo={styles.modalEnterTo}
            leave={styles.modalLeave}
            leaveFrom={styles.modalLeave}
            leaveTo={styles.modalLeaveTo}
          >
            <DialogPanel className={styles.modalContainer}>
              <DialogTitle className={styles.message}>{message}</DialogTitle>

              <div className={styles.buttonGroup}>
                <Button
                  variant={ButtonVariant.Default}
                  className={styles.button}
                  onClick={() => {
                    onPrimaryAction();
                    onClose();
                  }}
                >
                  {primaryButtonText}
                </Button>
                <Button
                  variant={ButtonVariant.Default}
                  className={styles.button}
                  onClick={() => {
                    onSecondaryAction();
                    onClose();
                  }}
                >
                  {secondaryButtonText}
                </Button>
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
