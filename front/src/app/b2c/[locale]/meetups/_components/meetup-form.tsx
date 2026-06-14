'use client';

import { useState, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { Camera, Plus, X, Users, Phone, Mail, Globe } from 'lucide-react';

import { Toggle } from '@/components/ui/toggle';
import { FormSegmentedControl } from '@/components/ui/form-segmented-control';
import { ModalMenu } from '@/components/ui/modal-menu';

import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { messengerIcons } from '@/components/ui/messenger-icons';
import { ImageCropperModal } from '@/components/ui/image-cropper-modal';
import { compressImageToBlob } from '@/lib/image-utils';
import { VENUE_COVER_MAX_SIZE_BYTES } from '@/lib/constants';
import stylesCommon from '@/components/ui/common.module.css';

export type MeetupFormValues = {
  title: string;
  description: string;
  startDateTime: string;
  maxParticipants: string;
  requiresApproval: boolean;
};

export interface EventContactForm {
  id: string;
  type: string;
  value: string;
  isPublic: boolean;
}

interface MeetupFormProps {
  pageTitle: string;
  userName: string;
  onCancel: () => void;
  initialIsSimple?: boolean;
  initialValues?: {
    title: string;
    description: string;
    startDateTime: string;
    coverImageUrl: string | null;
    contacts: EventContactForm[];
    hobbyIds: string[];
    maxParticipants: number | null;
    requiresApproval: boolean;
  };
  onSubmit: (data: {
    title: string;
    description: string;
    startDateTime: string;
    coverBlob: Blob | null;
    contacts: EventContactForm[];
    hobbyIds: string[];
    maxParticipants: number | null;
    requiresApproval: boolean;
  }) => Promise<void>;
  isSubmitting: boolean;
  submitLabel: string;
}

const contactTypes = [
  'Telegram',
  'WhatsApp',
  'Viber',
  'Phone',
  'Email',
] as const;

export function MeetupForm({
  pageTitle,
  userName,
  onCancel,
  initialIsSimple,
  initialValues,
  onSubmit,
  isSubmitting,
  submitLabel,
}: MeetupFormProps) {
  const t = useTranslations('components.forms.event');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isSimple, setIsSimple] = useState(initialIsSimple ?? true);
  const [participantMode, setParticipantMode] = useState<'tete' | 'custom'>(
    'tete',
  );

  const meetupSchema = z.object({
    title: z
      .string()
      .min(1, t('validation.titleRequired'))
      .max(200, t('validation.titleMaxLength')),
    description: isSimple
      ? z.string().optional()
      : z
          .string()
          .min(1, t('validation.descriptionRequired'))
          .max(2000, t('validation.descriptionMaxLength')),
    startDateTime: z.string().min(1, t('validation.startDateTimeRequired')),
    maxParticipants: z.string().optional(),
    requiresApproval: z.boolean(),
  });

  const [coverPreview, setCoverPreview] = useState<string | null>(
    initialValues?.coverImageUrl ?? null,
  );
  const [coverBlob, setCoverBlob] = useState<Blob | null>(null);
  const [rawImageSrc, setRawImageSrc] = useState<string | null>(null);
  const [isCropperOpen, setIsCropperOpen] = useState(false);
  const [contactTypeMenuId, setContactTypeMenuId] = useState<string | null>(null);
  const [contacts, setContacts] = useState<EventContactForm[]>(
    initialValues?.contacts ?? [],
  );
  const [contactErrors, setContactErrors] = useState<Record<string, string>>({});
  const [hobbyIds] = useState<string[]>(initialValues?.hobbyIds ?? []);

  const now = new Date();
  const nowLocal = new Date(now.getTime() - now.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<MeetupFormValues>({
    resolver: zodResolver(meetupSchema),
    defaultValues: {
      title: initialValues?.title ?? t('meetupTitle', { name: userName }),
      description: initialValues?.description ?? '',
      startDateTime: initialValues?.startDateTime ?? nowLocal,
      maxParticipants: initialValues?.maxParticipants?.toString() ?? '',
      requiresApproval: initialValues?.requiresApproval ?? true,
    },
  });

  const handleCoverClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      const reader = new FileReader();
      reader.onloadend = () => {
        setRawImageSrc(reader.result as string);
        setIsCropperOpen(true);
      };
      reader.readAsDataURL(file);

      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch {
      toast.error('Failed to process image');
    }
  };

  const handleCropComplete = async (croppedBlob: Blob) => {
    try {
      setCoverPreview(URL.createObjectURL(croppedBlob));

      const compressedBlob = await compressImageToBlob(
        croppedBlob,
        VENUE_COVER_MAX_SIZE_BYTES,
      );
      setCoverBlob(compressedBlob);
      setIsCropperOpen(false);
    } catch {
      toast.error('Failed to process cropped image');
    }
  };

  const addContact = () => {
    setContacts((prev) => [
      ...prev,
      {
        id: crypto.randomUUID(),
        type: 'Telegram',
        value: '',
        isPublic: false,
      },
    ]);
  };

  const validateContact = (type: string, value: string): string | null => {
    if (!value.trim()) return null;
    switch (type) {
      case 'Telegram':
        if (!/^[a-zA-Z0-9_]{3,32}$/.test(value)) return t('validation.contactTelegramInvalid');
        return null;
      case 'WhatsApp':
      case 'Viber':
      case 'Phone':
        if (!/^\+?\d{6,15}$/.test(value)) return t('validation.contactPhoneInvalid');
        return null;
      case 'Email':
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return t('validation.contactEmailInvalid');
        return null;
    }
    return null;
  };

  const validateAllContacts = (): boolean => {
    const errors: Record<string, string> = {};
    for (const c of contacts) {
      const err = validateContact(c.type, c.value);
      if (err) errors[c.id] = err;
    }
    setContactErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const getContactPlaceholder = (type: string): string => {
    switch (type) {
      case 'Telegram':
        return 'username';
      case 'Viber':
      case 'WhatsApp':
      case 'Phone':
        return '+38160123456';
      case 'Email':
        return 'email@example.com';
      default:
        return t('contactValue');
    }
  };

  const normalizeContactValue = (type: string, value: string): string => {
    switch (type) {
      case 'Telegram':
        return value
          .replace(/^@/, '')
          .replace(/^https?:\/\/t\.me\//, '')
          .replace(/\/$/, '')
          .trim();
      case 'Viber':
      case 'WhatsApp':
        return value.replace(/[^\d+]/g, '');
      default:
        return value;
    }
  };

  const updateContact = (
    id: string,
    field: keyof EventContactForm,
    value: string | boolean,
  ) => {
    if (field === 'value' && typeof value === 'string') {
      const contact = contacts.find((c) => c.id === id);
      if (contact) {
        value = normalizeContactValue(contact.type, value);
      }
      setContactErrors((prev) => {
        const next = { ...prev };
        delete next[id];
        return next;
      });
    }
    setContacts((prev) =>
      prev.map((c) => (c.id === id ? { ...c, [field]: value } : c)),
    );
  };

  const removeContact = (id: string) => {
    setContacts((prev) => prev.filter((c) => c.id !== id));
  };

  const onFormSubmit = async (values: MeetupFormValues) => {
    if (!isSimple && !coverBlob && !initialValues?.coverImageUrl) {
      toast.error(t('validation.coverRequired'));
      return;
    }

    if (!validateAllContacts()) {
      return;
    }

    const maxParticipants = isSimple
      ? participantMode === 'tete'
        ? 2
        : values.maxParticipants
          ? parseInt(values.maxParticipants, 10)
          : null
      : values.maxParticipants
        ? parseInt(values.maxParticipants, 10)
        : null;

    const requiresApproval = values.requiresApproval;

    await onSubmit({
      title: values.title,
      description: isSimple ? '' : values.description,
      startDateTime: values.startDateTime,
      coverBlob: isSimple ? null : coverBlob,
      contacts,
      hobbyIds,
      maxParticipants,
      requiresApproval,
    });
  };

  return (
    <div className="overflow-hidden sm:rounded-[16px]">
      <form
        onSubmit={handleSubmit(onFormSubmit)}
        className="flex flex-col gap-5"
      >
        {/* Cover image — advanced only */}
        {!isSimple && (
          <div className="w-full">
            <div
              className="relative h-[130px] w-full shrink-0 cursor-pointer"
              onClick={handleCoverClick}
            >
              <Header
                className="absolute! top-0! left-0! z-10! w-full! bg-transparent!"
                title={pageTitle}
                left={
                  <Icon
                    src="/assets/b2b/disclosure-left-icon.svg"
                    alt="back"
                    width={14}
                    height={27}
                    stopPropagation
                    onClick={onCancel}
                  />
                }
              />
              {coverPreview ? (
                <>
                  <img
                    src={coverPreview}
                    alt="Cover"
                    className="h-full w-full object-cover"
                  />
                  <div className="absolute inset-0 bg-black/60" />
                </>
              ) : (
                <div className="bg-form-element flex h-full w-full items-center justify-center">
                  <span className="relative inline-flex">
                    <Camera size={40} className="text-secondary opacity-50" />
                    <span className="text-primary absolute -top-1 -right-2 font-sans text-xl">
                      *
                    </span>
                  </span>
                </div>
              )}
              <div className="absolute right-3 bottom-3 flex h-10 w-10 items-center justify-center rounded-full bg-black/60">
                <Camera size={20} className="text-white" />
              </div>
              <input
                type="file"
                ref={fileInputRef}
                className="hidden"
                accept="image/*"
                onChange={handleFileChange}
              />
            </div>
          </div>
        )}

        {/* Simple mode header */}
        {isSimple && (
          <Header
            title={pageTitle}
            left={
              <Icon
                src="/assets/b2b/disclosure-left-icon.svg"
                alt="back"
                width={14}
                height={27}
                stopPropagation
                onClick={onCancel}
              />
            }
          />
        )}

        {/* Mode toggle */}
        <FormSegmentedControl
          options={[
            { value: 'simple', label: 'Simple' },
            { value: 'advanced', label: 'Advanced' },
          ]}
          value={isSimple ? 'simple' : 'advanced'}
          onChange={(v) => setIsSimple(v === 'simple')}
          className="sm:mx-4"
        />

        {/* Title */}
        <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
          <FormSectionTitle required>{t('titleField')}</FormSectionTitle>
          <Input
            {...register('title')}
            placeholder={t('titleField')}
            className={stylesCommon.inputDark}
          />
          {errors.title && (
            <p className={stylesCommon.errorMessage}>{errors.title.message}</p>
          )}
        </div>

        {/* Description — advanced only */}
        {!isSimple && (
          <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
            <FormSectionTitle required>{t('description')}</FormSectionTitle>
            <Textarea
              {...register('description')}
              placeholder={t('description')}
              rows={4}
              className={`${stylesCommon.inputDark} min-h-[120px] py-3`}
            />
            {errors.description && (
              <p className={stylesCommon.errorMessage}>
                {errors.description.message}
              </p>
            )}
          </div>
        )}

        {/* Date & Time */}
        <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
          <FormSectionTitle required>{t('startDateTime')}</FormSectionTitle>
          <Input
            type="datetime-local"
            {...register('startDateTime')}
            className={`${stylesCommon.inputDark} [color-scheme:dark]`}
          />
          {errors.startDateTime && (
            <p className={stylesCommon.errorMessage}>
              {errors.startDateTime.message}
            </p>
          )}
        </div>

        {/* Participants mode — simple only */}
        {isSimple && (
          <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
            <FormSectionTitle>{t('participants')}</FormSectionTitle>
            <FormSegmentedControl
              size="md"
              options={[
                {
                  value: 'tete',
                  label: (
                    <>
                      <Users size={16} />
                      Tête-à-tête
                    </>
                  ),
                },
                {
                  value: 'custom',
                  label: (
                    <>
                      <span className="flex">
                        <Users size={16} />
                        <Users size={16} />
                      </span>
                      {t('group')}
                    </>
                  ),
                },
              ]}
              value={participantMode}
              onChange={(v) => setParticipantMode(v as 'tete' | 'custom')}
            />
          </div>
        )}

        {/* Max participants input — simple custom or advanced */}
        {(participantMode === 'custom' || !isSimple) && (
          <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
            <FormSectionTitle>{t('maxParticipants')}</FormSectionTitle>
            <Input
              type="number"
              min={1}
              {...register('maxParticipants')}
              placeholder={t('maxParticipantsPlaceholder')}
              className={stylesCommon.inputDark}
            />
          </div>
        )}

        {/* Requires Approval */}
        <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
            <div className="flex items-center justify-between px-2 sm:px-0">
              <span className="text-sm text-white/80">
                {t('requiresApproval')}
              </span>
              <Toggle
                checked={watch('requiresApproval')}
                onChange={(v) => setValue('requiresApproval', v)}
              />
            </div>
          </div>

        {/* Contacts */}
        <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
          <div className="flex items-center justify-between">
            <FormSectionTitle>{t('contacts')}</FormSectionTitle>
            <button type="button" onClick={addContact} className="p-1">
              <Plus size={20} className="text-primary" />
            </button>
          </div>
          {contacts.map((contact) => (
            <div
              key={contact.id}
              className="bg-surface-dark mt-2 flex rounded-xl p-3"
            >
              <div className="flex flex-1 flex-col gap-2">
                <div className="flex items-center gap-5">
                  <div className="flex shrink-0 items-center gap-2">
                    <span className="text-sm text-white/50">
                      {t('contactIsPublic')}
                    </span>
                    <Toggle
                      checked={contact.isPublic}
                      onChange={(v) => updateContact(contact.id, 'isPublic', v)}
                    />
                  </div>
                  <div
                    className="bg-form-element flex h-10 flex-1 cursor-pointer items-center gap-2 rounded-xl px-3 text-sm"
                    onClick={() => setContactTypeMenuId(contact.id)}
                  >
                    <span style={{ width: 18, height: 18, display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }} className="shrink-0">
                      {messengerIcons[contact.type] ?? (contact.type === 'Phone' ? <Phone size={18} /> : contact.type === 'Email' ? <Mail size={18} /> : <Globe size={18} />)}
                    </span>
                    {contact.type}
                  </div>
                  <button
                    type="button"
                    onClick={() => removeContact(contact.id)}
                    className="flex h-10 items-center px-1"
                  >
                    <X size={18} className="text-error" />
                  </button>
                </div>
                <input
                  value={contact.value}
                  onChange={(e) =>
                    updateContact(contact.id, 'value', e.target.value)
                  }
                  placeholder={getContactPlaceholder(contact.type)}
                  className="bg-form-element placeholder:text-secondary h-[44px] w-full rounded-xl px-3 text-base outline-none"
                />
                {contactErrors[contact.id] && (
                  <p className="text-xs text-error">{contactErrors[contact.id]}</p>
                )}
                <p className="text-[11px] leading-tight text-white/40">
                  {contact.isPublic
                    ? 'Any user on a platform can see this contact information'
                    : 'Only joined users can see this contact information (after approval if enabled)'}
                </p>
              </div>
            </div>
          ))}

          <ModalMenu
            isOpen={!!contactTypeMenuId}
            onClose={() => setContactTypeMenuId(null)}
            options={contactTypes.map((ct) => ({
              label: ct,
              icon: <span style={{ width: 24, height: 24, display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}>
                {messengerIcons[ct] ?? (ct === 'Phone' ? <Phone size={24} /> : ct === 'Email' ? <Mail size={24} /> : <Globe size={24} />)}
              </span>,
              onClick: () => {
                if (contactTypeMenuId) {
                  updateContact(contactTypeMenuId, 'type', ct);
                }
                setContactTypeMenuId(null);
              },
            }))}
          />
        </div>

        <div className="mb-10 flex justify-center px-0 sm:px-4">
          <Button
            type="submit"
            disabled={isSubmitting}
            className="w-[280px]"
            variant={ButtonVariant.Default}
          >
            {isSubmitting ? <Spinner /> : submitLabel}
          </Button>
        </div>

        <ImageCropperModal
          isOpen={isCropperOpen}
          onClose={() => setIsCropperOpen(false)}
          imageSrc={rawImageSrc}
          onCropComplete={handleCropComplete}
          outputSize={{ width: 784, height: 208 }}
        />
      </form>
    </div>
  );
}
