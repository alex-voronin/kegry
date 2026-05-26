'use client';

import { useTranslations } from 'next-intl';
import { cn } from '@/lib/utils';

interface LoyaltyLevelData {
  id: number;
  visitsUpperBound: number;
}

interface LoyaltyLevelsTimelineProps {
  levels: LoyaltyLevelData[];
  onClick?: () => void;
  showTapHint?: boolean;
}

export function LoyaltyLevelsTimeline({
  levels,
  onClick,
  showTapHint = true,
}: LoyaltyLevelsTimelineProps) {
  const t = useTranslations('screens.companyProfile');

  if (levels.length === 0) {
    return (
      <div
        onClick={onClick}
        className="bg-form-element hover:bg-hover cursor-pointer sm:rounded-xl px-4 py-6 text-center text-sm text-secondary transition-colors"
      >
        {t('noActiveLevels')}
        {showTapHint && (
          <span className="font-medium">
            {' '}{t('tapToAdd')}
          </span>
        )}
      </div>
    );
  }

  const total = levels[levels.length - 1].visitsUpperBound;

  return (
    <div
      onClick={onClick}
      className="bg-form-element hover:bg-hover cursor-pointer sm:rounded-xl px-4 py-3 transition-colors"
    >
      <div className="flex items-start gap-3">
        {/* Left labels */}
        <div className="flex flex-col justify-between self-stretch pb-[1px]">
          <span className="text-xs leading-none text-secondary">
            {t('levelsLabel')}
          </span>
          <span className="text-xs leading-none text-secondary">
            {t('visitsLabel')}
          </span>
        </div>

        {/* Timeline */}
        <div className="flex-1">
          {/* Level numbers row */}
          <div className="relative mb-1 h-4">
            {levels.map((level, i) => {
              const prevBound = i === 0 ? 0 : levels[i - 1].visitsUpperBound;
              const center =
                ((prevBound + level.visitsUpperBound) / 2 / total) * 100;
              return (
                <span
                  key={level.id}
                  className="absolute text-sm font-medium -translate-x-1/2"
                  style={{ left: `${center}%` }}
                >
                  {level.id}
                </span>
              );
            })}
          </div>

          {/* Segmented bar */}
          <div className="flex h-2 overflow-hidden rounded-full">
            {levels.map((level, i) => {
              const prevBound = i === 0 ? 0 : levels[i - 1].visitsUpperBound;
              const width = level.visitsUpperBound - prevBound;
              return (
                <div
                  key={level.id}
                  className={cn(
                    'h-full',
                    i > 0 && 'border-l-2 border-form-element',
                    i === 0 && 'rounded-l-full',
                    i === levels.length - 1 && 'rounded-r-full',
                  )}
                  style={{
                    flex: width,
                    backgroundColor: 'var(--color-primary)',
                  }}
                />
              );
            })}
          </div>

          {/* Boundary values row */}
          <div className="relative mt-1 h-4">
            <span
              className="absolute text-xs text-secondary -translate-x-1/2"
              style={{ left: '0%' }}
            >
              0
            </span>
            {levels.slice(0, -1).map((level) => (
              <span
                key={level.id}
                className="absolute text-xs text-secondary -translate-x-1/2"
                style={{
                  left: `${(level.visitsUpperBound / total) * 100}%`,
                }}
              >
                {level.visitsUpperBound}
              </span>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
