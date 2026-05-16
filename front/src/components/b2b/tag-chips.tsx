import { cn } from '@/lib/utils';

export interface TagChipItem {
  id: string;
  label: string;
}

interface TagChipsProps {
  items: TagChipItem[];
  selectedIds: string[];
  onToggle: (id: string) => void;
  className?: string;
}

export function TagChips({
  items,
  selectedIds,
  onToggle,
  className,
}: TagChipsProps) {
  return (
    <div className={cn('flex flex-wrap gap-2', className)}>
      {items.map((item) => {
        const isSelected = selectedIds.includes(item.id);
        return (
          <button
            key={item.id}
            type="button"
            onClick={() => onToggle(item.id)}
            className={cn(
              'inline-flex items-center gap-1.5 rounded-full px-4 py-2 text-sm transition-colors',
              isSelected
                ? 'bg-active text-primary border-secondary border shadow-sm'
                : 'border-secondary text-secondary hover:bg-hover border',
            )}
          >
            {isSelected && (
              <svg
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="3"
                strokeLinecap="round"
                strokeLinejoin="round"
                className="shrink-0"
              >
                <polyline points="20 6 9 17 4 12" />
              </svg>
            )}
            <span className="max-w-[160px] truncate">{item.label}</span>
          </button>
        );
      })}
    </div>
  );
}
