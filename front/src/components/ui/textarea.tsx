import * as React from 'react';
import { cn } from '@/lib/utils';
import stylesCommon from './input.module.css';
import styles from './textarea.module.css';

const Textarea = React.forwardRef<
  HTMLTextAreaElement,
  React.ComponentProps<'textarea'>
>(({ className, ...props }, ref) => {
  return (
    <div className={stylesCommon.container}>
      <div className={stylesCommon.inputWrapper}>
        <textarea
          className={cn(stylesCommon.input, styles.textarea, className)}
          ref={ref}
          {...props}
        />
      </div>
    </div>
  );
});
Textarea.displayName = 'Textarea';

export { Textarea };
