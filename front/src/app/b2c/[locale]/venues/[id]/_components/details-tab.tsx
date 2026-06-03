'use client';

import Image from 'next/image';
import { useRouter } from '@/i18n/routing';
import { VenueTagAccordion } from '@/components/common/venue-tag-accordion';
import { FormSectionTitle } from '@/components/ui/form-section-title';
import { cn } from '@/lib/utils';
import { HelpCircle, ChevronRight } from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';
import { PromoIcon } from '@/lib/promo-types';
import { InstagramIcon } from './instagram-icon';
import { parseInstagramHandle } from '@/lib/url-utils';
import styles from '../venue-detail.module.css';
import type { VenueDto, CompanyDto, LoyaltyLevelWithDiscountDto } from '../use-venue-store';

function calculateProgressPercentage(count: number, lower: number, upper: number) {
  if (upper <= lower) return 100;
  return Math.max(0, Math.min(100, ((count - lower) / (upper - lower)) * 100));
}

interface DetailsTabProps {
  venue: VenueDto;
  company: CompanyDto;
  loyaltyLevel: LoyaltyLevelWithDiscountDto;
  venueId: string;
  t: (key: string, params?: Record<string, string | number | Date>) => string;
  onHelpOpen: () => void;
}

export function DetailsTab({ venue, company, loyaltyLevel, venueId, t, onHelpOpen }: DetailsTabProps) {
  const router = useRouter();

  return (
    <>
      {venue.tags?.length > 0 && (
        <div className={styles.section}>
          <VenueTagAccordion tagIds={venue.tags.map(t => t.id)} />
        </div>
      )}

      {(loyaltyLevel.discount !== 0 ||
        loyaltyLevel.isCustomDiscount ||
        loyaltyLevel.userLoyaltyLevel > 0 ||
        loyaltyLevel.visitsUpperBound != null) && (
        <div className={styles.section}>
          <FormSectionTitle>{t('myDiscount')}</FormSectionTitle>
          <div
            className={cn(styles.field, styles.loyaltyCard, styles.loyaltyCardInteractive)}
            onClick={() => router.push(PAGE_ROUTES.VENUE_DISCOUNTS(venueId))}
          >
            <div className={styles.loyaltyHeader}>
              <div className={styles.loyaltyLevelTitle}>
                <span>{t('level')} {loyaltyLevel.userLoyaltyLevel}</span>
              </div>
              <div className="flex items-center gap-2">
                {(loyaltyLevel.discount !== 0 || loyaltyLevel.isCustomDiscount) && (
                  <span className={styles.loyaltyDiscount}>
                    {loyaltyLevel.isCustomDiscount
                      ? t('offerAtVenue')
                      : t('discount', { percent: loyaltyLevel.discount ?? 0 })}
                  </span>
                )}
                <ChevronRight size={24} className="text-primary" />
              </div>
            </div>
            {(loyaltyLevel.visitsUpperBound !== null || loyaltyLevel.userLoyaltyLevel > 0) && (
              <div className={styles.progressContainer}>
                <div className={styles.progressBarBg}>
                  <div
                    className={styles.progressBarFill}
                    style={{
                      width: `${
                        loyaltyLevel.visitsUpperBound === null
                          ? 100
                          : calculateProgressPercentage(
                              loyaltyLevel.visitsCount,
                              loyaltyLevel.visitsLowerBound,
                              loyaltyLevel.visitsUpperBound,
                            )
                      }%`,
                    }}
                  />
                </div>
                <div className="flex items-center gap-1.5 self-end">
                  <div className={styles.progressText}>
                    {loyaltyLevel.visitsUpperBound === null
                      ? t('maxLoyaltyLevelReached')
                      : (() => {
                          const visitsText = t('visits', {
                            current: loyaltyLevel.visitsCount - loyaltyLevel.visitsLowerBound,
                            total: loyaltyLevel.visitsUpperBound - loyaltyLevel.visitsLowerBound,
                          });
                          const nextLevelDiscount = loyaltyLevel.nextLevelDiscount;
                          const showNextDiscount =
                            nextLevelDiscount !== null && nextLevelDiscount > (loyaltyLevel.discount ?? 0);
                          return showNextDiscount
                            ? `${visitsText} → ${t('nextDiscountInline', { percent: nextLevelDiscount })}`
                            : visitsText;
                        })()}
                  </div>
                  <div className="text-secondary cursor-pointer" onClick={(e) => { e.stopPropagation(); onHelpOpen(); }}>
                    <HelpCircle size={14} />
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      <div className={styles.section}>
        <FormSectionTitle>{t('address')}</FormSectionTitle>
        <div className={cn(styles.field, styles.actionField)} onClick={() => window.open(venue.googleMapsUrl, '_blank')}>
          <div className={styles.actionLabel}>
            <div className={styles.fieldIcon}>
              <Image src="/assets/venue/google-maps-icon.svg" alt="Maps" width={24} height={24} />
            </div>
            <span className={styles.fieldText}>{venue.address}</span>
          </div>
        </div>
      </div>

      {(venue.phoneNumber || venue.websiteUrl || venue.instagramUrl) && (
        <div className={styles.section}>
          <FormSectionTitle>{t('contacts')}</FormSectionTitle>
          {venue.phoneNumber && (
            <div className={styles.field}>
              <div className={styles.actionLabel}>
                <div className={styles.fieldIcon}>
                  <Image src="/assets/venue/phone-icon.svg" alt="Phone" width={24} height={24} />
                </div>
                <span>{venue.phoneNumber}</span>
              </div>
            </div>
          )}
          {venue.websiteUrl && (
            <div className={cn(styles.field, styles.actionField)} onClick={() => window.open(venue.websiteUrl as string, '_blank')}>
              <div className={styles.actionLabel}>
                <div className={styles.fieldIcon}>
                  <Image src="/assets/venue/website-icon.svg" alt="Website" width={24} height={24} />
                </div>
                <span className={styles.fieldText}>{venue.websiteUrl}</span>
              </div>
            </div>
          )}
          {venue.instagramUrl && (
            <div className={cn(styles.field, styles.actionField)} onClick={() => window.open(venue.instagramUrl as string, '_blank')}>
              <div className={styles.actionLabel}>
                <div className={styles.fieldIcon}>
                  <InstagramIcon />
                </div>
                <span className={styles.fieldText}>{parseInstagramHandle(venue.instagramUrl)}</span>
              </div>
            </div>
          )}
        </div>
      )}

      {venue.workingHours && (
        <div className={styles.section}>
          <FormSectionTitle>{t('workingHours')}</FormSectionTitle>
          <div className={styles.field}>
            <div className={styles.workingHoursText}>{venue.workingHours}</div>
          </div>
        </div>
      )}

      {venue.promos && venue.promos.length > 0 && (
        <div className={styles.section}>
          <FormSectionTitle>{t('promotions')}</FormSectionTitle>
          <div className={styles.promoList}>
            {venue.promos.map((promo, idx) => (
              <div key={idx} className={styles.promoItem}>
                <div className="flex items-center gap-3">
                  <PromoIcon type={promo.type} className="text-primary shrink-0" size={24} />
                  <div className={styles.promoBody}>{promo.body}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </>
  );
}
