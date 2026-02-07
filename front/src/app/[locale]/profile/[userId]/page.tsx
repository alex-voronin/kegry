'use client';

import { useState, useEffect } from 'react';
import Image from 'next/image';
import { useTranslations } from 'next-intl';
import { useRouter } from '@/i18n/routing';
import { useParams } from 'next/navigation';
import { Header } from '@/components/ui/header';
import { Icon } from '@/components/ui/icon';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { Spinner } from '@/components/ui/spinner';
import { cn } from '@/lib/utils';
import {
  useUserProfileStore,
  calculateAge,
  UserProfileDto,
} from '@/store/use-user-profile-store';
import styles from '../profile.module.css';

export default function UserProfilePage() {
  const params = useParams();
  const userId = params.userId as string;
  const router = useRouter();

  const tGender = useTranslations('components.profile-sections.gender');
  const tBio = useTranslations('components.profile-sections.bio');
  const tHobbies = useTranslations('components.profile-sections.hobbies');
  const tAge = useTranslations('components.profile-sections.age');

  const { fetchUserProfile } = useUserProfileStore();
  const [profile, setProfile] = useState<UserProfileDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadProfile = async () => {
      setIsLoading(true);
      const data = await fetchUserProfile(userId);
      setProfile(data);
      setIsLoading(false);
    };
    loadProfile();
  }, [userId, fetchUserProfile]);

  const handleBack = () => {
    router.back();
  };

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner />
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <span className="text-secondary">User not found</span>
      </div>
    );
  }

  const displayName = `${profile.firstName} ${profile.lastName}`;
  const age = profile.birthday ? calculateAge(profile.birthday) : null;
  const genderLabel = profile.gender
    ? tGender(profile.gender === 'male' ? 'male' : 'female')
    : '';

  return (
    <div>
      <Header
        left={
          <Icon
            src="/assets/common/disclosure-left-icon.svg"
            alt="back"
            width={14}
            height={27}
            onClick={handleBack}
          />
        }
      />

      <div className={styles.wrapper}>
        <div className={styles.avatarSection}>
          <div className={styles.avatarContainer}>
            <Image
              src={
                profile.avatarImageUrl ||
                '/assets/common/avatar-placeholder.webp'
              }
              alt="Avatar"
              fill
              className={styles.avatarImage}
              priority
            />
          </div>
          <h1 className={styles.userName}>{displayName}</h1>
        </div>

        <div className={styles.content}>
          <div className={styles.section}>
            <FormSectionTitle>{tGender('title')}</FormSectionTitle>
            <div className={styles.field}>
              <span>{genderLabel}</span>
            </div>
          </div>

          <div className={styles.section}>
            <FormSectionTitle>{tAge('title')}</FormSectionTitle>
            <div className={styles.field}>
              <span>{age ?? '--'}</span>
            </div>
          </div>

          <div className={styles.section}>
            <FormSectionTitle>{tBio('title')}</FormSectionTitle>
            <div className={styles.field}>
              <p>{profile.bio || '--'}</p>
            </div>
          </div>

          <div className={styles.section}>
            <FormSectionTitle>{tHobbies('title')}</FormSectionTitle>
            <div className={cn(styles.field, styles.hobbiesField)}>
              {profile.hobbies.map((hobby) => (
                <div key={hobby.id} className={styles.hobbyTag}>
                  {tHobbies(hobby.name as any) || hobby.name}
                </div>
              ))}
              {profile.hobbies.length === 0 && <span>--</span>}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
