import * as React from 'react';
import { cn } from '@/lib/utils';
import styles from './button.module.css';

export enum ButtonVariant {
  Default = 'default',
  Social = 'social',
}

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = ButtonVariant.Default, ...props }, ref) => {
    return (
      <button
        ref={ref}
        className={cn(
          styles.base,
          {
            // Variants
            [styles.variantDefault]: variant === ButtonVariant.Default,
            [styles.variantSocial]: variant === ButtonVariant.Social,
          },
          className,
        )}
        {...props}
      />
    );
  },
);
Button.displayName = 'Button';

export { Button };
