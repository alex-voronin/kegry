import * as React from 'react';
import { cn } from '@/lib/utils';
import { Spinner } from './spinner';
import styles from './button.module.css';

export enum ButtonVariant {
  Default = 'default',
  Social = 'social',
  Secondary = 'secondary',
  Destructive = 'destructive',
  Accent = 'accent',
}

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  loading?: boolean;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    { className, variant = ButtonVariant.Default, loading, children, ...props },
    ref,
  ) => {
    return (
      <button
        ref={ref}
        className={cn(
          styles.base,
          {
            [styles.variantDefault]: variant === ButtonVariant.Default,
            [styles.variantSocial]: variant === ButtonVariant.Social,
            [styles.variantSecondary]: variant === ButtonVariant.Secondary,
            [styles.variantDestructive]: variant === ButtonVariant.Destructive,
            [styles.variantAccent]: variant === ButtonVariant.Accent,
          },
          className,
        )}
        disabled={loading || props.disabled}
        {...props}
      >
        {loading ? <Spinner /> : children}
      </button>
    );
  },
);
Button.displayName = 'Button';

export { Button };
