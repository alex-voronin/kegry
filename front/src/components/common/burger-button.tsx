'use client';

import { Menu, AlertCircle } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

interface BurgerButtonProps {
  onOpen: () => void;
  hasRestrictedPermissions?: boolean;
  pendingApprovalsCount?: number;
}

export function BurgerButton({
  onOpen,
  hasRestrictedPermissions = false,
  pendingApprovalsCount = 0,
}: BurgerButtonProps) {
  return (
    <div className="bg-surface/85 relative flex h-12 w-[46px] cursor-pointer items-center justify-center rounded-full">
      <Menu size={28} className="text-primary" onClick={onOpen} />
      {pendingApprovalsCount > 0 ? (
        <Badge
          count={pendingApprovalsCount}
          className="absolute top-1 -right-px"
        />
      ) : hasRestrictedPermissions ? (
        <span className="bg-surface/85 absolute top-1 -right-px flex items-center justify-center rounded-full p-0.5">
          <AlertCircle
            size={16}
            strokeWidth={3}
            className="text-alert-vibrant"
          />
        </span>
      ) : null}
    </div>
  );
}
