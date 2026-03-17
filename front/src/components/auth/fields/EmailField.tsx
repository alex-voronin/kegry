import type React from 'react';
import { Input } from '@/components/ui/input';

export interface EmailFieldProps {
  placeholder: string;
  disabled?: boolean;
  className?: string;
  inputProps?: Omit<React.ComponentProps<typeof Input>, 'type' | 'placeholder' | 'disabled' | 'className'>;
}

export function EmailField({
  placeholder,
  disabled,
  className,
  inputProps,
}: EmailFieldProps) {
  return (
    <Input
      type="email"
      placeholder={placeholder}
      className={className}
      disabled={disabled}
      {...inputProps}
    />
  );
}


