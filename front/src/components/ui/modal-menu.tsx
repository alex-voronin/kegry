import {
  Dialog,
  DialogBackdrop,
  DialogPanel,
  Transition,
  TransitionChild,
} from '@headlessui/react';
import Image from 'next/image';
import { cn } from '@/lib/utils';
import styles from './modal-menu.module.css';

export interface ModalMenuOption {
  icon?: string;
  label: string;
  onClick: () => void;
  isDestructive?: boolean;
}

interface ModalMenuProps {
  isOpen: boolean;
  onClose: () => void;
  options: ModalMenuOption[];
  className?: string;
}

export function ModalMenu({
  isOpen,
  onClose,
  options,
  className,
}: ModalMenuProps) {
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

        <div className={styles.menuContainer}>
          <TransitionChild
            enter="translate-y-full"
            enterFrom="translate-y-full"
            enterTo="translate-y-0"
            leave="translate-y-0"
            leaveFrom="translate-y-0"
            leaveTo="translate-y-full"
          >
            <DialogPanel className={cn(styles.menuItems, className)}>
              <div className="flex flex-col">
                {options.map((option, index) => (
                  <button
                    key={index}
                    onClick={() => {
                      option.onClick();
                      onClose();
                    }}
                    className={cn(
                      styles.menuItem,
                      option.isDestructive && 'text-red-500',
                    )}
                  >
                    {option.icon && (
                      <div className={styles.icon}>
                        <Image
                          src={option.icon}
                          alt={option.label}
                          width={24}
                          height={24}
                          className="object-contain"
                        />
                      </div>
                    )}
                    <span className={styles.label}>{option.label}</span>
                  </button>
                ))}
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </Dialog>
    </Transition>
  );
}
