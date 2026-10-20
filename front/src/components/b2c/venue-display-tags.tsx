'use client';

import { useVenueTagChips } from '@/components/common/use-venue-tag-chips';
import styles from './venue-display-tags.module.css';

interface VenueDisplayTagsProps {
  tagIds: string[] | undefined;
  tagsEndpoint?: string;
}

const CATEGORY_CLASS: Record<string, string | undefined> = {
  DrinkCategory: styles.chipCatDrinkCategory,
  PriceLevel: styles.chipCatPriceLevel,
  HospitalityAndRules: styles.chipCatHospitalityAndRules,
  SmokingPolicy: styles.chipCatSmokingPolicy,
  FoodAndKitchen: styles.chipCatFoodAndKitchen,
  InfrastructureAndAmenities: styles.chipCatInfrastructureAndAmenities,
};

export function VenueDisplayTags({
  tagIds,
  tagsEndpoint,
}: VenueDisplayTagsProps) {
  const { visible, remaining, isEmpty } = useVenueTagChips(
    tagIds ?? [],
    tagsEndpoint,
    4,
  );

  if (isEmpty) return null;

  return (
    <div className={styles.row}>
      {visible.map(({ id, category, label }) => (
        <span
          key={id}
          className={`${styles.chip} ${CATEGORY_CLASS[category] ?? ''}`}
        >
          {label}
        </span>
      ))}
      {remaining > 0 && <span className={styles.moreBadge}>+{remaining}</span>}
    </div>
  );
}
