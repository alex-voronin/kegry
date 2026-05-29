'use client';

import { useState, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'react-toastify';
import { Camera, Plus, X } from 'lucide-react';

import { Button, ButtonVariant } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { ImageCropperModal } from '@/components/ui/image-cropper-modal';
import { compressImageToBlob } from '@/lib/image-utils';
import { VENUE_COVER_MAX_SIZE_BYTES } from '@/lib/constants';
import stylesCommon from '@/components/ui/common.module.css';

export type EventFormValues = {
  title: string;
  description: string;
  startDateTime: string;
};

export interface EventContactForm {
  id: string;
  type: string;
  value: string;
  isPublic: boolean;
}

interface EventFormProps {
  title: string;
  onCancel: () => void;
  initialValues?: {
    title: string;
    description: string;
    startDateTime: string;
    coverImageUrl: string | null;
    contacts: EventContactForm[];
    hobbyIds: string[];
  };
  onSubmit: (data: {
    title: string;
    description: string;
    startDateTime: string;
    coverBlob: Blob | null;
    contacts: EventContactForm[];
    hobbyIds: string[];
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

export function EventForm({
  title,
  onCancel,
  initialValues,
  onSubmit,
  isSubmitting,
  submitLabel,
}: EventFormProps) {
  const t = useTranslations('components.forms.event');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const eventSchema = z.object({
    title: z.string().min(1, t('validation.titleRequired')).max(200, t('validation.titleMaxLength')),
    description: z.string().min(1, t('validation.descriptionRequired')).max(2000, t('validation.descriptionMaxLength')),
    startDateTime: z.string().min(1, t('validation.startDateTimeRequired')),
  });
  const [coverPreview, setCoverPreview] = useState<string | null>(
    initialValues?.coverImageUrl ?? null,
  );
  const [coverBlob, setCoverBlob] = useState<Blob | null>(null);
  const [rawImageSrc, setRawImageSrc] = useState<string | null>(null);
  const [isCropperOpen, setIsCropperOpen] = useState(false);
  const [contacts, setContacts] = useState<EventContactForm[]>(
    initialValues?.contacts ?? [],
  );
  const [hobbyIds] = useState<string[]>(initialValues?.hobbyIds ?? []);

  const now = new Date();
  const nowLocal = new Date(now.getTime() - now.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<EventFormValues>({
    resolver: zodResolver(eventSchema),
    defaultValues: {
      title: initialValues?.title ?? '',
      description: initialValues?.description ?? '',
      startDateTime: initialValues?.startDateTime ?? nowLocal,
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
      { id: crypto.randomUUID(), type: 'Telegram', value: '', isPublic: false },
    ]);
  };

  const updateContact = (
    id: string,
    field: keyof EventContactForm,
    value: string | boolean,
  ) => {
    setContacts((prev) =>
      prev.map((c) => (c.id === id ? { ...c, [field]: value } : c)),
    );
  };

  const removeContact = (id: string) => {
    setContacts((prev) => prev.filter((c) => c.id !== id));
  };

  const onFormSubmit = async (values: EventFormValues) => {
    if (!coverBlob && !initialValues?.coverImageUrl) {
      toast.error(t('validation.coverRequired'));
      return;
    }
    await onSubmit({
      title: values.title,
      description: values.description,
      startDateTime: values.startDateTime,
      coverBlob,
      contacts,
      hobbyIds,
    });
  };

  return (
    <div className="overflow-hidden sm:rounded-[16px]">
      <form
        onSubmit={handleSubmit(onFormSubmit)}
        className="flex flex-col gap-5"
      >
        <div className="w-full">
          <div
            className="relative h-[130px] w-full shrink-0 cursor-pointer"
            onClick={handleCoverClick}
          >
            <Header
              className="absolute! top-0! left-0! z-10! w-full! bg-transparent!"
              title={title}
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

        {/* Description */}
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
                  className="mt-2 flex items-start gap-2 rounded-xl bg-surface-dark p-3"
                >
              <div className="flex flex-1 flex-col gap-2">
                <select
                  value={contact.type}
                  onChange={(e) =>
                    updateContact(contact.id, 'type', e.target.value)
                  }
                  className="h-10 w-full rounded-xl bg-form-element px-3 text-sm outline-none"
                >
                  {contactTypes.map((ct) => (
                    <option key={ct} value={ct}>
                      {ct}
                    </option>
                  ))}
                </select>
                <input
                  value={contact.value}
                  onChange={(e) =>
                    updateContact(contact.id, 'value', e.target.value)
                  }
                  placeholder={t('contactValue')}
                  className="h-[44px] w-full rounded-xl bg-form-element px-3 text-base outline-none placeholder:text-secondary"
                />
                <label className="text-secondary flex items-center gap-2 text-xs">
                  <input
                    type="checkbox"
                    checked={contact.isPublic}
                    onChange={(e) =>
                      updateContact(contact.id, 'isPublic', e.target.checked)
                    }
                  />
                  {t('contactIsPublic')}
                </label>
              </div>
              <button
                type="button"
                onClick={() => removeContact(contact.id)}
                className="p-1"
              >
                <X size={18} className="text-error" />
              </button>
            </div>
          ))}
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
