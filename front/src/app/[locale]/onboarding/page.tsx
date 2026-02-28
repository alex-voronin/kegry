'use client';

import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { Button, ButtonVariant } from '@/components/ui/button';
import { AvatarSection } from '@/components/ui/profile-sections/avatar-section';
import { HobbiesSection } from '@/components/ui/profile-sections/hobbies-section';
import { NameSection } from '@/components/ui/profile-sections/name-section';
import { BirthdaySection } from '@/components/ui/profile-sections/birthday-section';
import { GenderSection } from '@/components/ui/profile-sections/gender-section';
import { BioSection } from '@/components/ui/profile-sections/bio-section';
import styles from './onboarding.module.css';
import { Header } from '@/components/ui/header';
import { PAGE_ROUTES } from '@/lib/routes';
import { useUserProfileStore, Gender } from '@/store/use-user-profile-store';
import { calculateAge } from '@/lib/date';
import { useHobbiesStore } from '@/store/use-hobbies-store';
import { HobbyOption } from '@/components/ui/profile-sections/hobbies-section';

import { z } from 'zod';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'react-toastify';

export default function OnboardingPage() {
  const t = useTranslations('screens.onboarding');
  const router = useRouter();
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const tGender = useTranslations('components.profile-sections.gender');

  const {
    firstName,
    lastName,
    birthday,
    bio,
    gender,
    avatarBase64,
    setFirstName,
    setLastName,
    setBirthday,
    setBio,
    setGender,
    setAvatarBase64,
  } = useUserProfileStore();
  const { hobbies, selectedHobbyIds, setSelectedHobbyIds } = useHobbiesStore();

  const localizedSchema = z.object({
    firstName: z.string().trim().min(1, t('validation.firstNameRequired')),
    lastName: z.string().trim().min(1, t('validation.lastNameRequired')),
    birthday: z
      .string()
      .min(1, t('validation.birthdayRequired'))
      .refine((dateString) => {
        return calculateAge(dateString) >= 18;
      }, t('validation.minAge')),
    gender: z.string().min(1, t('validation.genderRequired')),
    bio: z.string().optional(),
    hobbies: z
      .array(z.string())
      .min(1, t('validation.minHobbies'))
      .max(10, t('validation.maxHobbies')),
  });

  type OnboardingFormValues = z.infer<typeof localizedSchema>;

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<OnboardingFormValues>({
    resolver: zodResolver(localizedSchema),
    defaultValues: {
      firstName: firstName || '',
      lastName: lastName || '',
      birthday: birthday || '',
      gender: gender || '',
      bio: bio || '',
      hobbies: selectedHobbyIds || [],
    },
    mode: 'onTouched',
  });

  const GENDER_OPTIONS = [
    { value: 'Male' as const, label: tGender('male'), width: 75 },
    { value: 'Female' as const, label: tGender('female'), width: 75 },
  ];

  const getSelectedHobbies = (ids: string[]): HobbyOption[] => {
    return hobbies
      .filter((h) => ids.includes(h.id))
      .map((h) => ({
        value: h.id,
        label: tHobbies(h.name),
      }));
  };

  const onSubmit = (data: OnboardingFormValues) => {
    if (!avatarBase64) {
      toast.error(t('validation.avatarRequired'));
      return;
    }
    router.push(PAGE_ROUTES.ONBOARDING_COVER);
  };

  return (
    <div className={styles.wrapper}>
      <Header title={t('title')} />

      <AvatarSection
        avatarBase64={avatarBase64}
        onAvatarChange={setAvatarBase64}
      />

      <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
        <div>
          <Controller
            control={control}
            name="firstName"
            render={({ field: { onChange, value } }) => (
              <Controller
                control={control}
                name="lastName"
                render={({ field: lastNameField }) => (
                  <NameSection
                    firstName={value}
                    lastName={lastNameField.value}
                    required
                    onFirstNameChange={(val) => {
                      onChange(val);
                      setFirstName(val);
                    }}
                    onLastNameChange={(val) => {
                      lastNameField.onChange(val);
                      setLastName(val);
                    }}
                  />
                )}
              />
            )}
          />
          {(errors.firstName || errors.lastName) && (
            <p className={styles.errorMessage}>
              {errors.firstName?.message || errors.lastName?.message}
            </p>
          )}
        </div>

        <div>
          <Controller
            control={control}
            name="birthday"
            render={({ field: { onChange, value } }) => (
              <BirthdaySection
                birthday={value}
                required
                onBirthdayChange={(val) => {
                  onChange(val);
                  setBirthday(val);
                }}
              />
            )}
          />
          {errors.birthday && (
            <p className={styles.errorMessage}>{errors.birthday.message}</p>
          )}
        </div>

        <div>
          <Controller
            control={control}
            name="gender"
            render={({ field: { onChange, value } }) => (
              <GenderSection
                options={GENDER_OPTIONS}
                value={value as Gender}
                required
                onChange={(val) => {
                  onChange(val);
                  setGender(val as Gender | null);
                }}
              />
            )}
          />
          {errors.gender && (
            <p className={styles.errorMessage}>{errors.gender.message}</p>
          )}
        </div>

        <div>
          <Controller
            control={control}
            name="bio"
            render={({ field: { onChange, value } }) => (
              <BioSection
                bio={value || ''}
                onBioChange={(val) => {
                  onChange(val);
                  setBio(val);
                }}
              />
            )}
          />
          {errors.bio && (
            <p className={styles.errorMessage}>{errors.bio.message}</p>
          )}
        </div>

        <div>
          <Controller
            control={control}
            name="hobbies"
            render={({ field: { onChange, value } }) => (
              <HobbiesSection
                value={getSelectedHobbies(value)}
                required
                onChange={(selected) => {
                  const ids = selected.map((h) => h.value);
                  onChange(ids);
                  setSelectedHobbyIds(ids);
                }}
              />
            )}
          />
          {errors.hobbies && (
            <p className={styles.errorMessage}>{errors.hobbies.message}</p>
          )}
        </div>

        <div className={styles.footer}>
          <Button
            variant={ButtonVariant.Default}
            className={styles.continueButton}
            type="submit"
          >
            {t('continue')}
          </Button>
        </div>
      </form>
    </div>
  );
}
