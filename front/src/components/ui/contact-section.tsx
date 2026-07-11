'use client';

import { useState, forwardRef, useImperativeHandle } from 'react';
import { useTranslations } from 'next-intl';
import { Plus, X, Phone, Mail, Globe } from 'lucide-react';

import { Toggle } from '@/components/ui/toggle';
import { ModalMenu } from '@/components/ui/modal-menu';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { messengerIcons } from '@/components/ui/messenger-icons';
import stylesCommon from '@/components/ui/common.module.css';

export interface EventContactForm {
  id: string;
  type: string;
  value: string;
  isPublic: boolean;
}

export const CONTACT_TYPES = [
  'Telegram',
  'WhatsApp',
  'Viber',
  'Phone',
  'Email',
] as const;

export interface ContactSectionHandle {
  getContacts: () => EventContactForm[];
  validate: () => boolean;
}

interface ContactSectionProps {
  initialContacts?: EventContactForm[];
}

export const ContactSection = forwardRef<
  ContactSectionHandle,
  ContactSectionProps
>(function ContactSection({ initialContacts }: ContactSectionProps, ref) {
  const t = useTranslations('components.forms.event');
  const [contactTypeMenuId, setContactTypeMenuId] = useState<string | null>(
    null,
  );
  const [contacts, setContacts] = useState<EventContactForm[]>(
    initialContacts ?? [],
  );
  const [contactErrors, setContactErrors] = useState<Record<string, string>>(
    {},
  );

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

  const removeContact = (id: string) => {
    setContacts((prev) => prev.filter((c) => c.id !== id));
  };

  const validateContact = (type: string, value: string): string | null => {
    if (!value.trim()) return null;
    switch (type) {
      case 'Telegram':
        if (!/^[a-zA-Z0-9_]{3,32}$/.test(value))
          return t('validation.contactTelegramInvalid');
        return null;
      case 'WhatsApp':
      case 'Viber':
      case 'Phone':
        if (!/^\+?\d{6,15}$/.test(value))
          return t('validation.contactPhoneInvalid');
        return null;
      case 'Email':
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value))
          return t('validation.contactEmailInvalid');
        return null;
    }
    return null;
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

  useImperativeHandle(ref, () => ({
    getContacts: () => contacts,
    validate: () => {
      const errors: Record<string, string> = {};
      for (const c of contacts) {
        const err = validateContact(c.type, c.value);
        if (err) errors[c.id] = err;
      }
      setContactErrors(errors);
      return Object.keys(errors).length === 0;
    },
  }));

  return (
    <div className={`${stylesCommon.sectionContainer} px-0 sm:px-4`}>
      <div className="flex items-center justify-between">
        <FormSectionTitle>{t('contacts')}</FormSectionTitle>
        <div
          onClick={addContact}
          className="bg-form-element flex size-7 cursor-pointer items-center justify-center rounded-full"
        >
          <Plus size={22} className="text-primary" />
        </div>
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
                <span
                  style={{
                    width: 18,
                    height: 18,
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                  className="shrink-0"
                >
                  {messengerIcons[contact.type] ??
                    (contact.type === 'Phone' ? (
                      <Phone size={18} />
                    ) : contact.type === 'Email' ? (
                      <Mail size={18} />
                    ) : (
                      <Globe size={18} />
                    ))}
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
              <p className="text-error text-xs">{contactErrors[contact.id]}</p>
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
        options={CONTACT_TYPES.map((ct) => ({
          label: ct,
          icon: (
            <span
              style={{
                width: 24,
                height: 24,
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              {messengerIcons[ct] ??
                (ct === 'Phone' ? (
                  <Phone size={24} />
                ) : ct === 'Email' ? (
                  <Mail size={24} />
                ) : (
                  <Globe size={24} />
                ))}
            </span>
          ),
          onClick: () => {
            if (contactTypeMenuId) {
              updateContact(contactTypeMenuId, 'type', ct);
            }
            setContactTypeMenuId(null);
          },
        }))}
      />
    </div>
  );
});
