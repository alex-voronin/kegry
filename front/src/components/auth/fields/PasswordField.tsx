import type React from 'react';
import { useState } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { Input } from '@/components/ui/input';

export interface PasswordFieldProps {
  placeholder: string;
  disabled?: boolean;
  className?: string;
  eyeButtonClassName?: string;
  ariaLabels?: { show: string; hide: string };
  showPassword?: boolean;
  onToggleShowPassword?: () => void;
  inputProps?: Omit<
    React.ComponentProps<typeof Input>,
    'type' | 'placeholder' | 'disabled' | 'className'
  >;
  extra?: React.ReactNode;
}

export function PasswordField({
  placeholder,
  disabled,
  className,
  eyeButtonClassName,
  ariaLabels = { show: 'Show password', hide: 'Hide password' },
  showPassword: controlledShowPassword,
  onToggleShowPassword,
  inputProps,
  extra,
}: PasswordFieldProps) {
  const [uncontrolledShowPassword, setUncontrolledShowPassword] =
    useState(false);
  const showPassword = controlledShowPassword ?? uncontrolledShowPassword;
  const toggle =
    onToggleShowPassword ?? (() => setUncontrolledShowPassword((v) => !v));

  return (
    <>
      <Input
        type={showPassword ? 'text' : 'password'}
        placeholder={placeholder}
        className={className}
        disabled={disabled}
        {...inputProps}
      />
      {extra ? (
        <div className="absolute top-1/2 right-0 -translate-y-1/2 flex items-center justify-end gap-2 pr-2">
          <button
            type="button"
            className={eyeButtonClassName}
            onClick={toggle}
            aria-label={showPassword ? ariaLabels.hide : ariaLabels.show}
          >
            {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
          </button>
          {extra}
        </div>
      ) : (
        <>
          <button
            type="button"
            className={eyeButtonClassName}
            onClick={toggle}
            aria-label={showPassword ? ariaLabels.hide : ariaLabels.show}
          >
            {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
          </button>
          {extra}
        </>
      )}
    </>
  );
}


