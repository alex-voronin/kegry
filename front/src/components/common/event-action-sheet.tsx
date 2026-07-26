'use client';

import { useState, useCallback } from 'react';
import { useRouter } from '@/i18n/routing';
import { useTranslations } from 'next-intl';
import { Eye, Pencil, Trash2, UserPlus, UserMinus } from 'lucide-react';
import { ModalMenu, type ModalMenuOption } from '@/components/ui/modal-menu';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { PAGE_ROUTES, API_ENDPOINTS } from '@/lib/routes';
import apiClient from '@/lib/api-client';
import { toast } from 'react-toastify';
import { useEventActions } from '@/hooks/use-event-actions';
import { OnboardingGuardDialog } from '@/components/common/onboarding-guard-dialog';
import { extractApiError } from '@/lib/api-error';

interface EventActionData {
  id: string;
  participationStatus?: string;
  creatorId: string;
  participantsCount: number;
  maxParticipants: number | null;
}

interface EventActionSheetProps {
  event: EventActionData | null;
  currentUserId?: string | null;
  onClose: () => void;
  onJoined: (eventId: string) => void;
  onLeft: (eventId: string) => void;
  onDeleted: (eventId: string) => void;
  onEdit: (eventId: string) => void;
  extraItems?: ModalMenuOption[];
  isAuthenticated?: boolean;
  onAuthRequired?: () => void;
}

export function EventActionSheet({
  event,
  currentUserId,
  onClose,
  onJoined,
  onLeft,
  onDeleted,
  onEdit,
  extraItems,
  isAuthenticated = true,
  onAuthRequired,
}: EventActionSheetProps) {
  const t = useTranslations('components.eventMenu');
  const router = useRouter();
  const [deleteEventId, setDeleteEventId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const {
    requireProfile,
    showOnboardingDialog,
    setShowOnboardingDialog,
    handleCreateProfile,
  } = useEventActions();

  const isOpen = !!event;
  const isCreator = event ? event.creatorId === currentUserId : false;
  const status = event?.participationStatus;
  const isFull =
    event !== null &&
    event.maxParticipants !== null &&
    event.participantsCount >= event.maxParticipants;

  const handleJoin = useCallback(async () => {
    if (!event) return;
    requireProfile(async () => {
      try {
        await apiClient.post(API_ENDPOINTS.EVENTS.JOIN(event.id));
        onJoined(event.id);
      } catch (error) {
        const [, code] = extractApiError(error);
        if (code === 'TooManyEntities') {
          toast.error(t('toast.eventFull'));
        } else {
          toast.error(t('toast.joinFailed'));
        }
      }
      onClose();
    });
  }, [event, onJoined, onClose, t, requireProfile]);

  const handleLeave = useCallback(async () => {
    if (!event) return;
    requireProfile(async () => {
      try {
        await apiClient.post(API_ENDPOINTS.EVENTS.LEAVE(event.id));
        onLeft(event.id);
      } catch {
        toast.error(t('toast.leaveFailed'));
      }
      onClose();
    });
  }, [event, onLeft, onClose, t, requireProfile]);

  const handleEdit = useCallback(() => {
    if (!event) return;
    onEdit(event.id);
    onClose();
  }, [event, onEdit, onClose]);

  const handleView = useCallback(() => {
    if (!event) return;
    router.push(PAGE_ROUTES.MEETUP_DETAIL(event.id));
    onClose();
  }, [event, router, onClose]);

  const handleDeleteConfirm = useCallback(async () => {
    if (!deleteEventId) return;
    setIsDeleting(true);
    try {
      await apiClient.delete(API_ENDPOINTS.EVENTS.DELETE(deleteEventId));
      onDeleted(deleteEventId);
      setDeleteEventId(null);
      onClose();
    } catch {
      toast.error(t('toast.deleteFailed'));
    } finally {
      setIsDeleting(false);
    }
  }, [deleteEventId, onDeleted, onClose, t]);

  const menuOptions: ModalMenuOption[] = [];

  if (extraItems) {
    menuOptions.push(...extraItems);
  }

  const guardAction = (action: () => void) => {
    if (!isAuthenticated && onAuthRequired) {
      return () => {
        onClose();
        onAuthRequired();
      };
    }
    return action;
  };

  if (isCreator) {
    menuOptions.push(
      {
        label: t('view'),
        onClick: handleView,
        icon: <Eye size={24} />,
      },
      {
        label: t('edit'),
        onClick: guardAction(handleEdit),
        icon: <Pencil size={24} />,
      },
      {
        label: t('delete'),
        onClick: guardAction(() => {
          setDeleteEventId(event!.id);
        }),
        icon: <Trash2 size={24} />,
        isDestructive: true,
      },
    );
  } else if (status === 'Approved') {
    menuOptions.push(
      {
        label: t('view'),
        onClick: handleView,
        icon: <Eye size={24} />,
      },
      {
        label: t('leave'),
        onClick: guardAction(handleLeave),
        icon: <UserMinus size={24} />,
      },
    );
  } else if (status === 'Pending' || status === 'Rejected') {
    menuOptions.push(
      {
        label: t('view'),
        onClick: handleView,
        icon: <Eye size={24} />,
      },
      {
        label: t('cancelRequest'),
        onClick: guardAction(handleLeave),
        icon: <UserMinus size={24} />,
      },
    );
  } else if (isFull) {
    menuOptions.push({
      label: t('view'),
      onClick: handleView,
      icon: <Eye size={24} />,
    });
  } else {
    menuOptions.push(
      {
        label: t('view'),
        onClick: handleView,
        icon: <Eye size={24} />,
      },
      {
        label: t('join'),
        onClick: guardAction(handleJoin),
        icon: <UserPlus size={24} />,
      },
    );
  }

  return (
    <>
      <ModalMenu isOpen={isOpen} onClose={onClose} options={menuOptions} />

      <ModalDialog
        isOpen={!!deleteEventId}
        onClose={() => setDeleteEventId(null)}
        message={t('deleteDialog.message')}
        primaryButtonText={t('deleteDialog.cancel')}
        secondaryButtonText={
          isDeleting ? t('deleteDialog.confirming') : t('deleteDialog.confirm')
        }
        onPrimaryAction={() => setDeleteEventId(null)}
        onSecondaryAction={handleDeleteConfirm}
      />

      <OnboardingGuardDialog
        isOpen={showOnboardingDialog}
        onClose={() => setShowOnboardingDialog(false)}
        onCreateProfile={handleCreateProfile}
      />
    </>
  );
}
