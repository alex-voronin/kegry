import * as React from 'react';
import { cn } from '@/lib/utils';
import styles from './input.module.css';

const Input = React.forwardRef<HTMLInputElement, React.ComponentProps<'input'>>(
  ({ className, type, ...props }, ref) => {
    return (
      <div className={styles.container}>
        <div className={styles.inputWrapper}>
          <input
            type={type}
            className={cn(styles.input, className)}
            ref={ref}
            {...props}
          />
        </div>
      </div>
    );
  },
);
Input.displayName = 'Input';

export { Input };
